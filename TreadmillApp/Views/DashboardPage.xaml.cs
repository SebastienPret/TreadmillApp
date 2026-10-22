using TreadmillApp.Services;
using TreadmillApp.Resources.Strings;
using TreadmillApp.Models;

namespace TreadmillApp.Views
{
    public partial class DashboardPage : ContentPage
    {
        public static TreadmillService Treadmill { get; } = new();

        public DashboardPage()
        {
            InitializeComponent();
            HorizontalStackLayoutManualControlsSpeed.IsEnabled = false;
            HorizontalStackLayoutManualControlsIncline.IsEnabled = false;
            GridStartStop.IsEnabled = false;

            Treadmill.PropertyChanged += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LblCurrentSpeed.Text = Treadmill.CurrentSpeed.ToString("0.0");
                    LblCurrentIncline.Text = Treadmill.CurrentIncline.ToString("0");
                });
            };

            Treadmill.IntervalStatusChanged += (s, status) =>
            {
                MainThread.BeginInvokeOnMainThread(() => LblStatus.Text = status);
            };

            Treadmill.DeviceStatusChanged += (s, status) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LblDeviceStatus.Text = status;
                    if (Treadmill.IsConnected)
                    {
                        HorizontalStackLayoutManualControlsSpeed.IsEnabled = true;
                        HorizontalStackLayoutManualControlsIncline.IsEnabled = true;
                        GridStartStop.IsEnabled = true;
                        BtnConnect.Text = AppResources.Btn_Connect; // Tapis Connecté ✅
                        BtnConnect.BackgroundColor = Colors.Green;
                    }
                    else
                    {
                        HorizontalStackLayoutManualControlsSpeed.IsEnabled = false;
                        HorizontalStackLayoutManualControlsIncline.IsEnabled = false;
                        GridStartStop.IsEnabled = false;
                        BtnConnect.Text = AppResources.Btn_Disconnect; // ⚠️ Tapis de course déconnecté
                        BtnConnect.BackgroundColor = Colors.Red;
                    }
                });
            };
        }

        private async void OnOpenScanClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ScanDevicesPage());

        private async void OnStartClicked(object sender, EventArgs e) => await Treadmill.StartAsync();
        private async void OnStopClicked(object sender, EventArgs e) => await Treadmill.StopAsync();

        private async void OnIncreaseSpeed(object sender, EventArgs e) => await Treadmill.SetSpeedAsync(Treadmill.CurrentSpeed + 0.5);
        private async void OnDecreaseSpeed(object sender, EventArgs e) => await Treadmill.SetSpeedAsync(Treadmill.CurrentSpeed - 0.5);

        private async void OnIncreaseIncline(object sender, EventArgs e) => await Treadmill.SetInclineAsync(Treadmill.CurrentIncline + 1);
        private async void OnDecreaseIncline(object sender, EventArgs e) => await Treadmill.SetInclineAsync(Treadmill.CurrentIncline - 1);

        private async void OnRunPlanClicked(object sender, EventArgs e)
        {
            var plan = IntervalsPage.CurrentPlan;
            if (plan.Intervals.Count == 0)
            {
                await DisplayAlertAsync(AppResources.Alert_NoIntervalsTitle, AppResources.Alert_NoIntervalsMessage, AppResources.Txt_OK); //"Vide", "Aucun intervalle configuré. Sélectionner ou créer un nouveau plan.", "OK"
                // got to IntervalsPage
                await Navigation.PushAsync(new SavedWorkoutsPage());
                //return;
            }

            _ = Task.Run(async () =>
            {
                await Treadmill.RunWorkoutPlanAsync(plan, (intervalName, secondsLeft) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        LblStatus.Text = $"{intervalName} : {secondsLeft}s {AppResources.Lbl_Restantes}"; //restantes
                    });
                });
            });
        }

        // Propriété textuelle pour l'affichage de l'allure (ex: "4:30 min/km")
        private async void PaceFormatted(object sender, EventArgs e)
        {
            string pace = LblCurrentSpeed.Text;
            LblCurrentPace.Text = Treadmill.PaceFormattedString(pace);
        }

        
    }
}