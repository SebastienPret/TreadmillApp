using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using TreadmillApp.Resources.Strings;
using TreadmillApp.Services;
using TreadmillApp.Views;

namespace TreadmillApp.ViewModels
{
    public partial class ScanDevicesViewModel : ObservableObject
    {
        private CancellationTokenSource? _connectCts;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBusy))]
        public partial bool IsScanning { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotConnecting))]
        [NotifyPropertyChangedFor(nameof(IsBusy))]
        public partial bool IsConnecting { get; set; }

        [ObservableProperty]
        public partial string ConnectingText { get; set; } = string.Empty;

        public bool IsNotConnecting => !IsConnecting;
        public bool IsBusy => IsScanning || IsConnecting;   // pour le spinner

        // On expose la liste des appareils détectés depuis le service/tapis
        public ObservableCollection<IDevice> DiscoveredDevices => DashboardPage.Treadmill.DiscoveredDevices;

        [RelayCommand]
        private async Task StartScan()
        {
            // 1. Permissions
            if (!await BlePermissionHelper.EnsurePermissionsAsync())
            {
                bool openSettings = await Shell.Current.DisplayAlertAsync(
                    AppResources.Txt_Permission_Required,
                    AppResources.Txt_Permission_Ble_Message,
                    AppResources.Txt_Open_Settings,
                    AppResources.Txt_Cancel);

                if (openSettings)
                    AppInfo.ShowSettingsUI();   // ouvre les paramètres de l'app

                return;
            }

            // 2. Bluetooth activé ?
            if (!BlePermissionHelper.IsBluetoothOn())
            {
                await Shell.Current.DisplayAlertAsync(
                    AppResources.Txt_Bluetooth_Off,
                    AppResources.Txt_Bluetooth_Off_Message,
                    AppResources.Txt_OK);
                return;
            }

            // 3. Scan
            try
            {
                IsScanning = true;
                await DashboardPage.Treadmill.StartScanningAsync();
            }
            finally
            {
                IsScanning = false;   // garanti même en cas d'exception
            }
        }

        [RelayCommand]
        private async Task DeviceSelected(IDevice selectedDevice)
        {
            if (selectedDevice == null || IsConnecting) return;
            _connectCts = new CancellationTokenSource();
            IsConnecting = true;
            ConnectingText = $"{AppResources.DeviceStatus_Connecting} {selectedDevice.Name}...";

            bool success = false;
            bool wasCancelled;

            try
            {
                success = await DashboardPage.Treadmill.ConnectToDeviceAsync(selectedDevice, _connectCts.Token);
            }
            finally
            {
                wasCancelled = _connectCts.IsCancellationRequested;
                _connectCts.Dispose();
                _connectCts = null;
                IsConnecting = false;
                ConnectingText = string.Empty;
            }

            if (!success && wasCancelled) return;   // annulation volontaire : pas d'alerte d'échec

            if (success)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Txt_Success, AppResources.Txt_Connection_Success, AppResources.Txt_OK);
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(
                    AppResources.Txt_Failure,
                    $"{AppResources.Txt_Connection_Failed}\n\n{DashboardPage.Treadmill.LastConnectionError}",
                    AppResources.Txt_OK);
            }
        }

        [RelayCommand]
        private void CancelConnect() => _connectCts?.Cancel();
    }
}