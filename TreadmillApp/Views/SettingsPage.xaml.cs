using TreadmillApp.Services;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var settings = AppSettingsService.Instance;

            // Sélectionner le bon index selon le code langue
            PickerLanguage.SelectedIndex = settings.SelectedLanguage switch
            {
                "fr-BE" => 1,
                "nl-BE" => 2,
                _ => 0  // "en" par défaut
            };

            SwCountdown.IsToggled = settings.EnableCountdown;
            SwAudio.IsToggled = settings.EnableAudioAlerts;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (PickerLanguage.SelectedIndex == -1) return;

            string selectedLangCode = PickerLanguage.SelectedIndex switch
            {
                1 => "fr-BE",
                2 => "nl-BE",
                _ => "en"
            };

            if (AppSettingsService.Instance.SelectedLanguage != selectedLangCode)
            {
                AppSettingsService.Instance.SelectedLanguage = selectedLangCode;

                // Recharger l'interface pour appliquer la nouvelle langue
                Application.Current!.Windows[0].Page = new AppShell();
            }
        }

        private void OnCountdownToggled(object sender, ToggledEventArgs e)
        {
            AppSettingsService.Instance.EnableCountdown = e.Value;
        }

        private void OnAudioToggled(object sender, ToggledEventArgs e)
        {
            AppSettingsService.Instance.EnableAudioAlerts = e.Value;
        }

        private async void OnResetDataClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlertAsync(
                AppResources.Txt_Confirmation,
                AppResources.Txt_ResetDataConfirmation,
                AppResources.Txt_Yes_rst,
                AppResources.Txt_Cancel);
            /*"Confirmation",
                "Voulez-vous vraiment effacer tous les programmes d'entraînement sauvegardés ?",
                "Oui, Réinitialiser",
                "Annuler"*/

            if (confirm)
            {
                Preferences.Remove("saved_workouts_list");
                await DisplayAlertAsync(AppResources.Txt_Success, AppResources.Txt_DataReset, AppResources.Txt_OK); //"Succès", "Toutes les données ont été réinitialisées.", "OK");
            }
        }
    }
}