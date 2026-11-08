using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Services
{
    public record PlatformAsset(string Url, string? Sha256);

    public record UpdateManifest(
        string Version,
        Dictionary<string, string>? Notes,
        PlatformAsset? Android,
        PlatformAsset? Windows,
        PlatformAsset? Ios);

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(UpdateManifest))]
    internal partial class UpdateJsonContext : JsonSerializerContext { }

    public static class UpdateService
    {
        private const string ManifestUrl =
            "https://raw.githubusercontent.com/SebastienPret/TreadmillApp/main/version.json";
        //"https://github.com/SebastienPret/TreadmillApp/releases/latest/download/version.json";

        private const string LastCheckKey = "update_last_check_utc";

        /// <summary>Mise à jour acceptée au démarrage, à installer depuis la page Paramètres.</summary>
        public static UpdateManifest? Pending { get; set; }

        private static bool IsCheckDue()
        {
            var ticks = Preferences.Default.Get(LastCheckKey, 0L);
            if (ticks == 0) return true;

            var elapsed = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
            return elapsed < TimeSpan.Zero || elapsed >= TimeSpan.FromHours(24);   // horloge reculée = on revérifie
        }


        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

        /// <summary>Retourne le manifeste si une version plus récente existe, sinon null.</summary>
        public static async Task<UpdateManifest?> CheckAsync(CancellationToken ct = default)
        {

            var json = await Http.GetStringAsync(ManifestUrl, ct);
            var manifest = JsonSerializer.Deserialize(json, UpdateJsonContext.Default.UpdateManifest);
            if (manifest == null || !Version.TryParse(manifest.Version, out var remote)) return null;
            if (!Version.TryParse(AppInfo.Current.VersionString, out var local)) return null;

            return Normalize(remote) > Normalize(local) ? manifest : null;

        }

        // "1.0" et "1.0.0" doivent être considérées comme égales
        private static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0));

        public static string GetNotes(UpdateManifest m)
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (m.Notes != null)
            {
                if (m.Notes.TryGetValue(lang, out var n)) return n;
                if (m.Notes.TryGetValue("en", out var en)) return en;
            }
            return string.Empty;
        }

        public static async Task DownloadAndInstallAsync(
            UpdateManifest m, IProgress<double>? progress = null, CancellationToken ct = default)
        {
#if ANDROID
            var asset = m.Android;
#elif WINDOWS
            var asset = m.Windows;
#else
            var asset = m.Ios;
#endif
            if (asset == null) throw new InvalidOperationException("Aucun fichier pour cette plateforme.");

#if IOS || MACCATALYST
            await Launcher.OpenAsync(asset.Url);   // iOS : pas d'installation directe possible
#else
            var fileName = Path.GetFileName(new Uri(asset.Url).LocalPath);
            var path = await DownloadAsync(asset.Url, fileName, progress, ct);
            await VerifyAsync(path, asset.Sha256);
            Install(path);
#endif
        }

#if !IOS && !MACCATALYST
        private static async Task<string> DownloadAsync(
            string url, string fileName, IProgress<double>? progress, CancellationToken ct)
        {
            var dir = Path.Combine(FileSystem.CacheDirectory, "updates");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName);

            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? -1;

            await using var src = await response.Content.ReadAsStreamAsync(ct);
            await using var dst = File.Create(path);

            var buffer = new byte[81920];
            long read = 0;
            int n;
            while ((n = await src.ReadAsync(buffer, ct)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, n), ct);
                read += n;
                if (total > 0) progress?.Report((double)read / total);
            }
            return path;
        }

        private static async Task VerifyAsync(string path, string? expectedSha256)
        {
            if (string.IsNullOrWhiteSpace(expectedSha256)) return;

            await using var fs = File.OpenRead(path);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(fs));
            if (!hash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(path);
                throw new InvalidOperationException("Fichier corrompu (empreinte SHA-256 différente).");
            }
        }
#endif

#if ANDROID
        private static void Install(string path)
        {
            var context = Android.App.Application.Context;

            // Android 8+ : autorisation « installer des apps inconnues » à accorder à ton app
            if (OperatingSystem.IsAndroidVersionAtLeast(26) &&
                !context.PackageManager!.CanRequestPackageInstalls())
            {
                var settings = new Android.Content.Intent(
                    Android.Provider.Settings.ActionManageUnknownAppSources,
                    Android.Net.Uri.Parse("package:" + context.PackageName));
                settings.AddFlags(Android.Content.ActivityFlags.NewTask);
                context.StartActivity(settings);
                throw new InvalidOperationException(
                    "Autorise l'installation depuis cette app dans les réglages, puis relance la mise à jour.");
            }

            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                context, context.PackageName + ".fileprovider", new Java.IO.File(path))!;

            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetDataAndType(uri, "application/vnd.android.package-archive");
            intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission |
                            Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
#elif WINDOWS
        private static void Install(string path)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            Application.Current?.Quit();   // libère les fichiers pour l'installeur
        }
#endif


        // Automatic startup check: off in Debug builds, on in Release builds
        private static bool AutoCheckEnabled =>
#if DEBUG
            false;
#else
            true;
#endif

        /// <summary>À appeler au démarrage : vérifie au plus une fois par 24 h, sans jamais déranger en cas d'erreur.</summary>
        public static async Task CheckOnStartupAsync()
        {
            if (!AutoCheckEnabled) return;

            try
            {
                if (!IsCheckDue()) return;

                var update = await CheckAsync();   // échoue sans réseau : on n'enregistre rien, on réessaiera au prochain lancement
                Preferences.Default.Set(LastCheckKey, DateTime.UtcNow.Ticks);

                if (update == null) return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var shell = Shell.Current;
                    if (shell == null) return;

                    bool accept = await shell.DisplayAlertAsync(
                        $"{AppResources.Txt_Update_Available} {update.Version}",
                        GetNotes(update),
                        AppResources.Btn_Update,
                        AppResources.Txt_Later);

                    if (!accept) return;

#if IOS || MACCATALYST
            await DownloadAndInstallAsync(update);   // ouvre simplement le lien
#else
                    Pending = update;                        // la page Paramètres affiche la progression
                    await shell.GoToAsync("//SettingsPage");
#endif
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Vérification de mise à jour : {ex.Message}");
            }
        }
    }
}