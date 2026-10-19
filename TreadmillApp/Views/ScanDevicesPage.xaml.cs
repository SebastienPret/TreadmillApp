using Plugin.BLE.Abstractions.Contracts;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Views
{
    public partial class ScanDevicesPage : ContentPage
    {
        public ScanDevicesPage()
        {
            InitializeComponent();
            CvDevices.ItemsSource = DashboardPage.Treadmill.DiscoveredDevices;
        }

        private async void OnScanClicked(object sender, EventArgs e)
        {
            Loader.IsRunning = true;
            await DashboardPage.Treadmill.StartScanningAsync();
            Loader.IsRunning = false;
        }

        private async void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is IDevice selectedDevice)
            {
                Loader.IsRunning = true;
                bool success = await DashboardPage.Treadmill.ConnectToDeviceAsync(selectedDevice);
                Loader.IsRunning = false;

                if (success)
                {
                    await DisplayAlertAsync(AppResources.Txt_Success, AppResources.Txt_Connection_Success, AppResources.Txt_OK); //"Succès", "Connecté au tapis avec succès !", "OK"
                    await Navigation.PopAsync();
                }
                else
                {
                    // donner un retour à l'utilisateur si la connexion échoue
                    await DisplayAlertAsync(AppResources.Txt_Failure, AppResources.Txt_Connection_Failed, AppResources.Txt_OK); //"Échec", "Impossible de se connecter à cet appareil.", "OK"
                    // Vous pouvez également envisager de rester sur la page de scan pour permettre à l'utilisateur de réessayer
                    // ou de sélectionner un autre appareil
                    // Par exemple, vous pouvez réinitialiser la sélection pour permettre à l'utilisateur de choisir un autre appareil
                    CvDevices.SelectedItem = null; // Réinitialiser la sélection
                }
            }
        }
    }
}