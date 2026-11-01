using TreadmillApp.Models;
using TreadmillApp.ViewModels;

namespace TreadmillApp.Views
{
    public partial class IntervalsPage : ContentPage
    {
        //public static Models.WorkoutPlan CurrentPlan { get; set; } = new();

        private readonly IntervalsViewModel? _viewModel;

        public IntervalsPage()
        {
            InitializeComponent();
            _viewModel = BindingContext as IntervalsViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Rafraîchit les données du ViewModel avec le nouveau CurrentPlan sélectionné
            _viewModel?.LoadCurrentPlan();
        }

        private void OnDeleteIntervalClicked(object? sender, EventArgs e)
        {
            if (sender is Button { BindingContext: WorkoutInterval interval })
            {
                _viewModel?.DeleteIntervalCommand.Execute(interval);
            }
        }

        private void OnEditIntervalClicked(object? sender, EventArgs e)
        {
            if (sender is Button { BindingContext: WorkoutInterval interval })
            {
                _viewModel?.EditIntervalCommand.Execute(interval);
            }
        }
    }
}