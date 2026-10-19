using System.Collections.ObjectModel;
using TreadmillApp.Models;
using TreadmillApp.Services;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Views
{
    public partial class SavedWorkoutsPage : ContentPage
    {
        private ObservableCollection<WorkoutPlan> _savedWorkouts = new();

        public SavedWorkoutsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadWorkouts();
        }

        private void LoadWorkouts()
        {
            _savedWorkouts = WorkoutStorageService.LoadWorkouts();
            CvSavedWorkouts.ItemsSource = _savedWorkouts;
        }

        /* private async void OnWorkoutSelected(object sender, SelectionChangedEventArgs e)
         {
             if (e.CurrentSelection.FirstOrDefault() is WorkoutPlan selectedPlan)
             {
                 // Définit ce plan comme le plan actif
                 IntervalsPage.CurrentPlan = selectedPlan;

                 bool loadAndRun = await DisplayAlertAsync("Programme sélectionné",
                     $"Voulez-vous charger '{selectedPlan.Title}' pour l'utiliser immédiatement ?",
                     "Oui, lancer", "Fermer");

                 if (loadAndRun)
                 {
                     await Shell.Current.GoToAsync("//DashboardPage");
                 }

                 CvSavedWorkouts.SelectedItem = null; // Réinitialise la sélection
             }
         }

         private async void OnGoToCreatorClicked(object sender, EventArgs e)
         {
             // Redirige vers l'onglet du créateur d'intervalles
             await Shell.Current.GoToAsync("//IntervalsPage");
         }*/
        private async void OnRunWorkoutClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is WorkoutPlan plan)
            {
                IntervalsPage.CurrentPlan = plan;
                await Shell.Current.GoToAsync("//DashboardPage");
            }
        }

        private async void OnEditWorkoutClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is WorkoutPlan plan)
            {
                // Charge le plan dans le créateur pour le modifier
                IntervalsPage.CurrentPlan = plan;
                await Shell.Current.GoToAsync("//IntervalsPage");
            }
        }

        private async void OnDeleteWorkoutClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is WorkoutPlan plan)
            {
                bool confirm = await DisplayAlertAsync(AppResources.Confirmation, AppResources.Txt_Want_Delete + " " + plan.Title + " ?" , AppResources.Txt_Yes, AppResources.Txt_No); //"Confirmation", $"Voulez-vous vraiment supprimer '{plan.Title}' ?", "Oui", "Non"
                if (confirm)
                {
                    WorkoutStorageService.DeleteWorkout(plan);
                    LoadWorkouts(); // Rafraîchit la liste
                }
            }
        }

        private async void OnCreateNewClicked(object sender, EventArgs e)
        {
            // Réinitialise avec un nouveau plan vide
            IntervalsPage.CurrentPlan = new WorkoutPlan { Title = AppResources.DefaultWorkoutTitle }; //"Nouvel Entraînement"
            await Shell.Current.GoToAsync("//IntervalsPage");
        }


    }
}