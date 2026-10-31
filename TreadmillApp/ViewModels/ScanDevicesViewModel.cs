using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using TreadmillApp.Resources.Strings;
using TreadmillApp.Views;

namespace TreadmillApp.ViewModels
{
    public partial class ScanDevicesViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsScanning { get; set; } = false;

        // On expose la liste des appareils détectés depuis le service/tapis
        public static ObservableCollection<IDevice> DiscoveredDevices => DashboardPage.Treadmill.DiscoveredDevices;

        [RelayCommand]
        private async Task StartScan()
        {
            IsScanning = true;
            await DashboardPage.Treadmill.StartScanningAsync();
            IsScanning = false;
        }

        [RelayCommand]
        private async Task DeviceSelected(IDevice selectedDevice)
        {
            if (selectedDevice == null) return;
            IsScanning = true;

            // Affichage de l'alerte de progression (Note: DisplayAlert nécessite le Shell ou une page active)
            // Comme DisplayAlert est une méthode de Page, on peut utiliser Shell.Current pour l'afficher globalement

            bool success = await DashboardPage.Treadmill.ConnectToDeviceAsync(selectedDevice);
            IsScanning = false;

            if (success)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Txt_Success, AppResources.Txt_Connection_Success, AppResources.Txt_OK);
                await Shell.Current.GoToAsync(".."); // Équivalent de PopAsync() avec le Shell
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Txt_Failure, AppResources.Txt_Connection_Failed, AppResources.Txt_OK);
            }
        }
    }
}