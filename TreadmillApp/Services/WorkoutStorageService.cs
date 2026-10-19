// Services/WorkoutStorageService.cs
using System.Collections.ObjectModel;
using System.Text.Json;
using TreadmillApp.Models;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Services
{
    public static class WorkoutStorageService
    {
        private const string StorageKey = "saved_workouts_list";

        // Charge tous les entraînements sauvegardés
        public static ObservableCollection<WorkoutPlan> LoadWorkouts()
        {
            var json = Preferences.Get(StorageKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                // Entraînement par défaut si la liste est vide
                var defaultPlan = new WorkoutPlan
                {
                    Title = AppResources.DefaultWorkoutTitle, //Fractionné Basique
                    Intervals = new List<WorkoutInterval>
                    {
                        new() { Name = AppResources.Interval_EasyRun, TargetSpeed = 6.0, TargetIncline = 0, DurationSeconds = 120 }, //Échauffement
                        new() { Name = AppResources.Interval_Sprint, TargetSpeed = 12.0, TargetIncline = 2, DurationSeconds = 30 }, //Sprint
                        new() { Name = AppResources.Interval_CoolDown, TargetSpeed = 6.0, TargetIncline = 0, DurationSeconds = 60 } //Récupération
                    }
                };
                var list = new ObservableCollection<WorkoutPlan> { defaultPlan };
                SaveWorkouts(list);
                return list;
            }

            try
            {
                var plans = JsonSerializer.Deserialize<ObservableCollection<WorkoutPlan>>(json);
                return plans ?? new ObservableCollection<WorkoutPlan>();
            }
            catch
            {
                return new ObservableCollection<WorkoutPlan>();
            }
        }

        // Sauvegarde la liste complète
        public static void SaveWorkouts(ObservableCollection<WorkoutPlan> plans)
        {
            var json = JsonSerializer.Serialize(plans);
            Preferences.Set(StorageKey, json);
        }

        // Ajoute ou met à jour un entraînement
        public static void SaveWorkout(WorkoutPlan plan)
        {
            var plans = LoadWorkouts();
            // Évite les doublons basés sur le titre ou ajoute-le
            var existing = plans.FirstOrDefault(p => p.Title.Equals(plan.Title, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                plans.Remove(existing);
            }
            plans.Add(plan);
            SaveWorkouts(plans);
        }

        // Supprime un entraînement
        public static void DeleteWorkout(WorkoutPlan plan)
        {
            var plans = LoadWorkouts();
            var target = plans.FirstOrDefault(p => p.Title.Equals(plan.Title, StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                plans.Remove(target);
                SaveWorkouts(plans);
            }
        }
    }
}