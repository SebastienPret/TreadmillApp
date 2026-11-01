using TreadmillApp.Models;
using TreadmillApp.Services;
using TreadmillApp.ViewModels;

namespace TreadmillApp.Views
{

    public partial class SavedWorkoutsPage : ContentPage
    {
        private readonly SavedWorkoutsViewModel _viewModel;
        public SavedWorkoutsPage()
        {
            InitializeComponent();
            _viewModel = BindingContext as SavedWorkoutsViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            //_viewModel?.LoadWorkouts();
            AppStateService.Instance.LoadWorkouts();
        }

        private void OnRunClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is WorkoutPlan plan)
            {
                _viewModel?.RunWorkoutCommand.Execute(plan);
            }
        }

        private void OnEditClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is WorkoutPlan plan)
            {
                _viewModel?.EditWorkoutCommand.Execute(plan);
            }
        }

        private void OnDeleteClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is WorkoutPlan plan)
            {
                _viewModel?.DeleteWorkoutCommand.Execute(plan);
            }
        }
    }
}