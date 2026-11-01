using TreadmillApp.Resources.Themes;

namespace TreadmillApp.Services
{
    public enum AppStyle
    {
        Dark,
        Light,
        Ocean
    }

    public static class ThemeService
    {
        private const string PrefKey = "app_style";

        public static AppStyle Current { get; private set; } = AppStyle.Dark;

        /// <summary>À appeler une fois au démarrage (App constructor).</summary>
        public static void Initialize()
        {
            var saved = Preferences.Default.Get(PrefKey, nameof(AppStyle.Dark));
            var style = Enum.TryParse(saved, out AppStyle parsed) ? parsed : AppStyle.Dark;
            Apply(style, save: false);
        }

        /// <summary>Applique un style immédiatement et le mémorise.</summary>
        public static void Apply(AppStyle style, bool save = true)
        {
            var app = Application.Current;
            if (app == null) return;

            ResourceDictionary theme = style switch
            {
                AppStyle.Light => new LightTheme(),
                AppStyle.Ocean => new OceanTheme(),
                _ => new DarkTheme()
            };

            var merged = app.Resources.MergedDictionaries;

            // Retire l'ancien thème
            var old = merged.FirstOrDefault(d => d is DarkTheme or LightTheme or OceanTheme);
            if (old != null)
                merged.Remove(old);

            merged.Add(theme);

            // Aligne les éléments système (barre d'état, clavier, popups)
            app.UserAppTheme = style == AppStyle.Light ? AppTheme.Light : AppTheme.Dark;

            Current = style;
            ApplyShellColors();
            ApplySystemBars(style);

            if (save)
                Preferences.Default.Set(PrefKey, style.ToString());
        }


        private static void ApplySystemBars(AppStyle style)
        {
#if ANDROID
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
                    if (window?.DecorView == null) return;

                    // Icônes foncées en thème clair, claires sinon
                    var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
                    if (controller != null)
                        controller.AppearanceLightStatusBars = style == AppStyle.Light;

                    // Couleur de fond de la barre d'état (sans effet sur Android 15+)
                    if (Application.Current?.Resources.TryGetValue("SurfaceColor", out var value) == true && value is Color color)
                    {
                        window.ClearFlags(Android.Views.WindowManagerFlags.TranslucentStatus);
                        window.AddFlags(Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds);
#pragma warning disable CA1422
                        window.SetStatusBarColor(Microsoft.Maui.Platform.ColorExtensions.ToPlatform(color));
#pragma warning restore CA1422
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Barre d'état : {ex.Message}");
                }
            });
#endif
        }

        /// <summary>Lie les couleurs du Shell aux ressources du thème (suit le changement de style).</summary>
        public static void ApplyShellColors(Shell? shell = null)
        {
            shell ??= Shell.Current;
            if (shell == null) return;

            shell.SetDynamicResource(Shell.BackgroundColorProperty, "SurfaceColor");
            shell.SetDynamicResource(Shell.ForegroundColorProperty, "AccentColor");
            shell.SetDynamicResource(Shell.TitleColorProperty, "TextPrimary");
            shell.SetDynamicResource(Shell.UnselectedColorProperty, "TextSecondary");
            shell.SetDynamicResource(Shell.TabBarBackgroundColorProperty, "SurfaceColor");
            shell.SetDynamicResource(Shell.TabBarForegroundColorProperty, "AccentColor");
            shell.SetDynamicResource(Shell.TabBarTitleColorProperty, "AccentColor");
            shell.SetDynamicResource(Shell.TabBarUnselectedColorProperty, "TextSecondary");
        }
    }
}
