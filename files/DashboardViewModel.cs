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

        // Indicateurs spécifiques à l'elliptique
        [ObservableProperty]
        public partial string CurrentResistance { get; set; } = "0";
        [ObservableProperty]
        public partial string CurrentCadence { get; set; } = "0";
        [ObservableProperty]
        public partial string CurrentPower { get; set; } = "0";

        // Type d'appareil : 0 = tapis, 1 = elliptique (index du Picker)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTreadmill))]
        [NotifyPropertyChangedFor(nameof(IsCrossTrainer))]
        public partial int MachineTypeIndex { get; set; } = (int)Treadmill.MachineType;

        public bool IsTreadmill => MachineTypeIndex == (int)MachineType.Treadmill;
        public bool IsCrossTrainer => MachineTypeIndex == (int)MachineType.CrossTrainer;

        public List<string> MachineTypeNames { get; } =
            [AppResources.Machine_Treadmill, AppResources.Machine_CrossTrainer];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotConnected))] // Met à jour IsNotConnected automatiquement si IsConnected change
        public partial bool IsConnected { get; set; }
        public bool IsNotConnected => !IsConnected;
        [ObservableProperty]
        public partial string ConnectButtonText { get; set; } = AppResources.Btn_Search_Treadmill;


        // set style of the button to start with a value from the theme (blue for light, green for dark, etc.)

        //[ObservableProperty]
        //public partial Color ConnectButtonBackgroundColor { get; set; } // = Color.FromArgb("#2196F3");

        

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
                    CurrentResistance = Treadmill.CurrentResistance.ToString("0.#");
                    CurrentCadence = Treadmill.CurrentCadence.ToString("0");
                    CurrentPower = Treadmill.CurrentPower.ToString("0");
                    MachineTypeIndex = (int)Treadmill.MachineType;   // suit la détection automatique
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
                    //ConnectButtonBackgroundColor = IsConnected ? Colors.Green : Colors.Red;
                });
            };
        }

        // Changement manuel du type d'appareil (Picker)
        partial void OnMachineTypeIndexChanged(int value)
        {
            Treadmill.MachineType = (MachineType)value;
        }

        // Action déclenchée lorsque CurrentSpeed change (généré automatiquement par le Toolkit)
        partial void OnCurrentSpeedChanged(string value)
        {
            CurrentPace = Treadmill.PaceFormattedString(value);
        }

        [RelayCommand]
        private async Task OpenScan() => await Shell.Current.GoToAsync(nameof(ScanDevicesPage));  //Navigation.PushAsync(new ScanDevicesPage());

        [RelayCommand]
        private static async Task DecreaseSpeed() => await Treadmill.SetSpeedAsync(Treadmill.CurrentSpeed - 0.5);

        [RelayCommand]
        private static async Task IncreaseSpeed() => await Treadmill.SetSpeedAsync(Treadmill.CurrentSpeed + 0.5);

        [RelayCommand]
        private static async Task DecreaseIncline() => await Treadmill.SetInclineAsync(Treadmill.CurrentIncline - 1);

        [RelayCommand]
        private static async Task IncreaseIncline() => await Treadmill.SetInclineAsync(Treadmill.CurrentIncline + 1);

        [RelayCommand]
        private static async Task DecreaseResistance() => await Treadmill.SetResistanceAsync(Treadmill.CurrentResistance - 1);

        [RelayCommand]
        private static async Task IncreaseResistance() => await Treadmill.SetResistanceAsync(Treadmill.CurrentResistance + 1);

        [RelayCommand]
        private static async Task Start() => await Treadmill.StartAsync();

        [RelayCommand]
        private static async Task Stop() => await Treadmill.StopAsync();

        [RelayCommand]
        private async Task RunPlan()
        {
            if (Treadmill.IsRunning) return;   // déjà en cours

            //var plan = IntervalsPage.CurrentPlan;
            var plan = AppStateService.Instance.CurrentPlan;
            if (plan == null || plan.Intervals.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Alert_NoIntervalsTitle, AppResources.Alert_NoIntervalsMessage, AppResources.Txt_OK);
                await Shell.Current.Navigation.PushAsync(new SavedWorkoutsPage());
                return;
            }

            if (!Treadmill.IsConnected)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Error, AppResources.Error_Not_Connected, AppResources.Txt_OK);
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