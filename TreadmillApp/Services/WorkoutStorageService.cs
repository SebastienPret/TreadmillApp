using System.Collections.ObjectModel;
using System.Text.Json;
using TreadmillApp.Models;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Services;

public static class WorkoutStorageService
{
    private const string StorageKey = "saved_workouts_list";

    public static ObservableCollection<WorkoutPlan> LoadWorkouts()
    {
        var json = Preferences.Get(StorageKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            var defaultPlan = new WorkoutPlan
            {
                Title = AppResources.DefaultWorkoutTitle,
                Intervals = new ObservableCollection<WorkoutInterval>
                {
                    new() { Name = AppResources.Interval_EasyRun, TargetSpeed = 6.0, TargetIncline = 0, DurationSeconds = 120 },
                    new() { Name = AppResources.Interval_Sprint, TargetSpeed = 12.0, TargetIncline = 2, DurationSeconds = 30 },
                    new() { Name = AppResources.Interval_CoolDown, TargetSpeed = 6.0, TargetIncline = 0, DurationSeconds = 60 }
                }
            };

            var defaultWorkouts = new ObservableCollection<WorkoutPlan> { defaultPlan };
            SaveWorkouts(defaultWorkouts);
            return defaultWorkouts;
        }

        try
        {
            return JsonSerializer.Deserialize<ObservableCollection<WorkoutPlan>>(json)
                ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static void SaveWorkouts(ObservableCollection<WorkoutPlan> plans)
    {
        ArgumentNullException.ThrowIfNull(plans);

        var json = JsonSerializer.Serialize(plans);
        Preferences.Set(StorageKey, json);
    }

    public static void SaveWorkout(WorkoutPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var plans = LoadWorkouts();
        var existing = plans.FirstOrDefault(p =>
            string.Equals(p.Title, plan.Title, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            plans.Remove(existing);
        }

        plans.Add(plan);
        SaveWorkouts(plans);
    }

    public static void DeleteWorkout(WorkoutPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var plans = LoadWorkouts();
        var target = plans.FirstOrDefault(p =>
            string.Equals(p.Title, plan.Title, StringComparison.OrdinalIgnoreCase));

        if (target is not null)
        {
            plans.Remove(target);
            SaveWorkouts(plans);
        }
    }
}
