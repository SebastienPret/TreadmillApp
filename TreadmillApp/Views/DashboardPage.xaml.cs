using TreadmillApp.Services;
using TreadmillApp.ViewModels;

namespace TreadmillApp.Views
{
    public partial class DashboardPage : ContentPage
    {
        public static TreadmillService Treadmill { get; } = new();

        public DashboardPage()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // TEMPORAIRE : affiche l'erreur à l'écran pour la lire en Release
                Content = new ScrollView
                {
                    Content = new Label
                    {
                        Text = ex.ToString(),
                        Margin = 20,
                        FontSize = 12
                    }
                };
            }
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