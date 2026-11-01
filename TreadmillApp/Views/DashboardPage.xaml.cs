using TreadmillApp.Services;
using TreadmillApp.ViewModels;

namespace TreadmillApp.Views
{
    public partial class DashboardPage : ContentPage
    {
        public static TreadmillService Treadmill { get; } = new();

        public DashboardPage()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (AppStateService.Instance.AutoStartWorkout)
            {
                AppStateService.Instance.AutoStartWorkout = false;   // une seule fois

                if (BindingContext is DashboardViewModel vm)
                    vm.RunPlanCommand.Execute(null);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Only if you don't want the screen to stay on when navigating away
            //DeviceDisplay.Current.KeepScreenOn = false;

            
        }
    }
}