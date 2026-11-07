using System.Diagnostics;
using TreadmillApp.Models;
using TreadmillApp.ViewModels;

namespace TreadmillApp.Views
{
    public partial class IntervalsPage : ContentPage
    {
        //public static Models.WorkoutPlan CurrentPlan { get; set; } = new();

        private readonly IntervalsViewModel _viewModel;

        public IntervalsPage()
        {
            try
            {
                InitializeComponent();
                _viewModel = new IntervalsViewModel(); //BindingContext as IntervalsViewModel;
                BindingContext = _viewModel;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'initialisation de la page IntervalsPage : {ex.Message}");
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
                //throw; // Relance l'exception pour ne pas masquer l'erreur
            }
            
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