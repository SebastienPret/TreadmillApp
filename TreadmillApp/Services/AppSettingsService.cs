// Services/AppSettingsService.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TreadmillApp.Services
{
    public class AppSettingsService : INotifyPropertyChanged
    {
        private const string KeyLanguage = "app_language";
        private const string KeyEnableCountdown = "enable_countdown";
        private const string KeyEnableAudioAlerts = "enable_audio_alerts";

        private static AppSettingsService? _instance;
        public static AppSettingsService Instance => _instance ??= new AppSettingsService();

        public string SelectedLanguage
        {
            get => Preferences.Get(KeyLanguage, "fr-BE"); // "fr", "fr-BE", "nl-BE", "en"
            set
            {
                Preferences.Set(KeyLanguage, value);
                LocalizationService.SetLanguage(value);
                OnPropertyChanged();
            }
        }

        public bool EnableCountdown
        {
            get => Preferences.Get(KeyEnableCountdown, true);
            set
            {
                Preferences.Set(KeyEnableCountdown, value);
                OnPropertyChanged();
            }
        }

        public bool EnableAudioAlerts
        {
            get => Preferences.Get(KeyAudioAlertsKey(), true);
            set
            {
                Preferences.Set(KeyAudioAlertsKey(), value);
                OnPropertyChanged();
            }
        }

        private static string KeyAudioAlertsKey() => KeyEnableAudioAlerts;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}