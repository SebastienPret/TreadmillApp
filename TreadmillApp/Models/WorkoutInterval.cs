using TreadmillApp.Resources.Strings;
// Models/WorkoutInterval.cs

namespace TreadmillApp.Models
{
    public class WorkoutInterval
    {
        public string Name { get; set; } = AppResources.DefaultIntervalName; // ex: "Sprint", "Récupération", "Effort"
        public double TargetSpeed { get; set; } = 10.0; // km/h
        public double TargetIncline { get; set; } = 1.0; // %
        public int DurationSeconds { get; set; } = 60; // Durée en secondes

        // Propriétés pour la pause automatique
        public bool EnableRest { get; set; } = false;
        public int RestSecondsAfter { get; set; } = 10; // Durée de la pause si activée
    }

    public class WorkoutPlan
    {
        public string Title { get; set; } = AppResources.DefaultWorkoutTitle; //"Mon Entraînement"
        public List<WorkoutInterval> Intervals { get; set; } = new();
    }
}