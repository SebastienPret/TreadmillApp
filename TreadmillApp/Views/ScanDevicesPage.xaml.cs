using Plugin.BLE.Abstractions.Contracts;
using TreadmillApp.ViewModels;

namespace TreadmillApp.Views
{
    public partial class ScanDevicesPage : ContentPage
    {
        private readonly ScanDevicesViewModel _viewModel;

        public ScanDevicesPage()
        {
            InitializeComponent();
            _viewModel = BindingContext as ScanDevicesViewModel;
        }

        private async void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is IDevice selectedDevice)
            {
                // Exécute la commande du ViewModel avec l'appareil sélectionné
                await _viewModel?.DeviceSelectedCommand.ExecuteAsync(selectedDevice);

                // Réinitialise la sélection pour permettre de re-cliquer si besoin
                CvDevices.SelectedItem = null;
            }
        }
    }
}