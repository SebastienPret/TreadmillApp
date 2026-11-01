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

            if (save)
                Preferences.Default.Set(PrefKey, style.ToString());
        }
    }
}
