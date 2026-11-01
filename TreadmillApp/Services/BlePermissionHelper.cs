using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace TreadmillApp.Services
{
    public static class BlePermissionHelper
    {
        /// <summary>Vérifie / demande les permissions nécessaires au scan BLE.</summary>
        public static async Task<bool> EnsurePermissionsAsync()
        {
            // Android 12+ : BLUETOOTH_SCAN / BLUETOOTH_CONNECT. iOS : autorisation Bluetooth.
            var status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Bluetooth>();

            if (status != PermissionStatus.Granted)
                return false;

#if ANDROID
            // Android 11 et moins : le scan BLE exige la permission de localisation
            if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var loc = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (loc != PermissionStatus.Granted)
                    loc = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (loc != PermissionStatus.Granted)
                    return false;
            }
#endif
            return true;
        }

        /// <summary>Le Bluetooth est-il activé sur l'appareil ?</summary>
        public static bool IsBluetoothOn() =>
            CrossBluetoothLE.Current.State == BluetoothState.On;
    }
}