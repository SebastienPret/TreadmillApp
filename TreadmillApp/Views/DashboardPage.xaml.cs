using TreadmillApp.Services;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Views
{
    public partial class DashboardPage : ContentPage
    {
        public static TreadmillService Treadmill { get; } = new();

        public DashboardPage()
        {
            InitializeComponent();

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
                        BtnConnect.Text = AppResources.Btn_Connect; // Tapis Connecté ✅
                        BtnConnect.BackgroundColor = Colors.Green;
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
                await DisplayAlertAsync(AppResources.Alert_NoIntervalsTitle, AppResources.Alert_NoIntervalsMessage, AppResources.Txt_OK); //"Vide", "Aucun intervalle configuré. Allez dans l'onglet Intervalles.", "OK"
                return;
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
    }
}