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
            BindingContext = _viewModel;
        }
        private async void OnAppearing(object sender, EventArgs e)
        {
            base.OnAppearing();
            await Task.Delay(2000);   // laisse le temps à l'UI de se mettre à jour avant de lancer le scan
        }

        private async void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null || _viewModel.IsConnecting) return;

            if (e.CurrentSelection.FirstOrDefault() is IDevice selectedDevice)
            {
                await _viewModel.DeviceSelectedCommand.ExecuteAsync(selectedDevice);
                CvDevices.SelectedItem = null;   // permet de re-sélectionner après un échec ou une annulation
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel?.CancelConnectCommand.Execute(null);   // sans effet si aucune connexion en cours
        }
    }
}