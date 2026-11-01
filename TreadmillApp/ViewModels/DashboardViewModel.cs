using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE.Abstractions;
using System.Diagnostics;
using TreadmillApp.Models;
using TreadmillApp.Resources.Strings;
using TreadmillApp.Services;
using TreadmillApp.Views;

namespace TreadmillApp.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        public static TreadmillService Treadmill => DashboardPage.Treadmill; // Ou injecté via DI
        // [ObservableProperty] va générer automatiquement la propriété publique "DeviceStatus"
        [ObservableProperty]
        public partial string DeviceStatus { get; set; } = AppResources.Lbl_Device_Status;

        [ObservableProperty]
        public partial string CurrentSpeed { get; set; } = "0,0";
        [ObservableProperty]
        public partial string CurrentPace { get; set; } = "0:00";
        [ObservableProperty]
        public partial string CurrentIncline { get; set; } = "0";
        [ObservableProperty]
        public partial string StatusRun { get; set; } = AppResources.Lbl_Status_Run;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotConnected))] // Met à jour IsNotConnected automatiquement si IsConnected change
        public partial bool IsConnected { get; set; }
        public bool IsNotConnected => !IsConnected;
        [ObservableProperty]
        public partial string ConnectButtonText { get; set; } = AppResources.Btn_Search_Treadmill;

        [ObservableProperty]
        public partial Color ConnectButtonBackgroundColor { get; set; } = Color.FromArgb("#2196F3");

        

        // Les commandes s'écrivent aussi plus proprement avec l'attribut [RelayCommand]
        public DashboardViewModel()
        {
            // Écoute des événements du service Treadmill (reste identique)
            Treadmill.PropertyChanged += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CurrentSpeed = Treadmill.CurrentSpeed.ToString("0.0");
                    CurrentIncline = Treadmill.CurrentIncline.ToString("0");
                });
            };

            Treadmill.IntervalStatusChanged += (s, status) =>
            {
                MainThread.BeginInvokeOnMainThread(() => StatusRun = status);
            };

            Treadmill.DeviceStatusChanged += (s, status) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DeviceStatus = status;
                    IsConnected = Treadmill.IsConnected;

                    ConnectButtonText = IsConnected ? AppResources.Btn_Connect : AppResources.Btn_Disconnect;
                    ConnectButtonBackgroundColor = IsConnected ? Colors.Green : Colors.Red;
                });
            };
        }

        // Action déclenchée lorsque CurrentSpeed change (généré automatiquement par le Toolkit)
        partial void OnCurrentSpeedChanged(string value)
        {
            CurrentPace = Treadmill.PaceFormattedString(value);
        }

        [RelayCommand]
        private static async Task OpenScan() => await Shell.Current.Navigation.PushAsync(new ScanDevicesPage());

        [RelayCommand]
        private static async Task DecreaseSpeed() => await Treadmill.SetSpeedAsync(Treadmill.CurrentSpeed - 0.5);

        [RelayCommand]
        private static async Task IncreaseSpeed() => await Treadmill.SetSpeedAsync(Treadmill.CurrentSpeed + 0.5);

        [RelayCommand]
        private static async Task DecreaseIncline() => await Treadmill.SetInclineAsync(Treadmill.CurrentIncline - 1);

        [RelayCommand]
        private static async Task IncreaseIncline() => await Treadmill.SetInclineAsync(Treadmill.CurrentIncline + 1);

        [RelayCommand]
        private static async Task Start() => await Treadmill.StartAsync();

        [RelayCommand]
        private static async Task Stop() => await Treadmill.StopAsync();

        [RelayCommand]
        private async Task RunPlan()
        {
            //var plan = IntervalsPage.CurrentPlan;
            var plan = AppStateService.Instance.CurrentPlan;
            if (plan == null || plan.Intervals.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Alert_NoIntervalsTitle, AppResources.Alert_NoIntervalsMessage, AppResources.Txt_OK);
                await Shell.Current.Navigation.PushAsync(new SavedWorkoutsPage());
                return;
            }

            _ = Task.Run(async () =>
            {
                await Treadmill.RunWorkoutPlanAsync(plan, (intervalName, secondsLeft) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StatusRun = $"{intervalName} : {secondsLeft}s {AppResources.Lbl_Restantes}";
                    });
                });
            });
        }
    }
}