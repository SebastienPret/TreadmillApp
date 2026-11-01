using TreadmillApp.Services;

namespace TreadmillApp.Views
{
    public partial class DashboardPage : ContentPage
    {
        public static TreadmillService Treadmill { get; } = new();

        public DashboardPage()
        {
            InitializeComponent();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Only if you don't want the screen to stay on when navigating away
            //DeviceDisplay.Current.KeepScreenOn = false;
        }
    }
}