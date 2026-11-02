using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TreadmillApp.Resources.Strings;
using TreadmillApp.Services;

namespace TreadmillApp.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial int SelectedLanguageIndex { get; set; } = 0;

        [ObservableProperty]
        public partial bool EnableCountdown { get; set; } = false;

        [ObservableProperty]
        public partial bool EnableAudioAlerts { get; set; } = false;

        //[ObservableProperty]
        public string VersionText => $"{AppInfo.Current.VersionString} (FTMS BLE)";

        public SettingsViewModel()
        {
            LoadSettings();
        }

        private bool _isLoading;

        public void LoadSettings()
        {
            _isLoading = true;
            try
            {
                var settings = AppSettingsService.Instance;

                SelectedLanguageIndex = settings.SelectedLanguage switch
                {
                    "fr-BE" => 1,
                    "nl-BE" => 2,
                    _ => 0
                };

                EnableCountdown = settings.EnableCountdown;
                EnableAudioAlerts = settings.EnableAudioAlerts;
            }
            finally
            {
                _isLoading = false;
            }
        }

        partial void OnSelectedLanguageIndexChanged(int value)
        {
            if (_isLoading || value == -1) return;

            string selectedLangCode = value switch
            {
                1 => "fr-BE",
                2 => "nl-BE",
                _ => "en"
            };

            var settings = AppSettingsService.Instance;
            if (settings.SelectedLanguage != selectedLangCode)
            {
                settings.SelectedLanguage = selectedLangCode;

                // Recharger l'interface pour appliquer la nouvelle langue
                Application.Current!.Windows[0].Page = new AppShell();
            }
        }

        partial void OnEnableCountdownChanged(bool value)
        {
            if (_isLoading) return;
            AppSettingsService.Instance.EnableCountdown = value;
        }

        partial void OnEnableAudioAlertsChanged(bool value)
        {
            if (_isLoading) return;
            AppSettingsService.Instance.EnableAudioAlerts = value;
        }

        [RelayCommand]
        private static async Task ResetData()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                AppResources.Txt_Confirmation,
                AppResources.Txt_ResetDataConfirmation,
                AppResources.Txt_Yes_rst,
                AppResources.Txt_Cancel);

            if (confirm)
            {
                Preferences.Remove("saved_workouts_list");
                await Shell.Current.DisplayAlertAsync(
                    AppResources.Txt_Success,
                    AppResources.Txt_DataReset,
                    AppResources.Txt_OK);
            }
        }
    }
}