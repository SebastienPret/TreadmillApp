using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using TreadmillApp.Models;
using TreadmillApp.Resources.Strings;
using TreadmillApp.Services;
using TreadmillApp.Views;

namespace TreadmillApp.ViewModels
{
    public partial class SavedWorkoutsViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial ObservableCollection<WorkoutPlan> SavedWorkouts { get; set; } = new();

        // Le constructeur charge les entraînements dès l'ouverture de la page
        public SavedWorkoutsViewModel()
        {
            LoadWorkouts();
        }

        // Méthode pour charger ou rafraîchir la liste (appelée depuis le ViewModel ou le code-behind lors de l'apparition)
        public void LoadWorkouts()
        {
            try
            {
                var workouts = WorkoutStorageService.LoadWorkouts();

                // Protection cruciale : si le service renvoie null, on initialise une liste vide
                if (workouts != null)
                {
                    SavedWorkouts = new ObservableCollection<WorkoutPlan>(workouts);
                }
                else
                {
                    SavedWorkouts = [];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur chargement entraînements : {ex.Message}");
                SavedWorkouts = [];
            }
        }

        [RelayCommand]
        private static async Task RunWorkout(WorkoutPlan plan)
        {
            if (plan == null) return;

            IntervalsPage.CurrentPlan = plan;
            await Shell.Current.GoToAsync("//DashboardPage");
        }

        [RelayCommand]
        private static async Task EditWorkout(WorkoutPlan plan)
        {
            if (plan == null) return;

            IntervalsPage.CurrentPlan = plan;
            await Shell.Current.GoToAsync("//IntervalsPage");
            // IMPORTANT : Réinitialise la sélection pour permettre de re-sélectionner le même ou un autre
            IntervalsPage.CurrentPlan = null; 
        }

        [RelayCommand]
        private async Task DeleteWorkout(WorkoutPlan plan)
        {
            if (plan == null) return;

            bool confirm = await Shell.Current.DisplayAlertAsync(
                AppResources.Confirmation,
                $"{AppResources.Txt_Want_Delete} {plan.Title} ?",
                AppResources.Txt_Yes,
                AppResources.Txt_No);

            if (confirm)
            {
                WorkoutStorageService.DeleteWorkout(plan);
                LoadWorkouts(); // Rafraîchit la liste après suppression
            }
        }

        [RelayCommand]
        private static async Task CreateNew()
        {
            IntervalsPage.CurrentPlan = new WorkoutPlan { Title = AppResources.DefaultWorkoutTitle };
            await Shell.Current.GoToAsync("//IntervalsPage");
        }
    }
}