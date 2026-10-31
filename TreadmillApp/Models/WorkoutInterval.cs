using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Models;

public partial class WorkoutInterval : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = AppResources.DefaultIntervalName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaceFormatted))]
    [NotifyPropertyChangedFor(nameof(PaceFormattedString))]
    public partial double TargetSpeed { get; set; } = 10.0;

    //public string PaceFormattedString => PaceFormatted;
    [ObservableProperty]
    public partial string PaceFormattedString { get; set; } = "0:00 min/km";

    [ObservableProperty]
    public partial double TargetIncline { get; set; } = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationMinutes))]
    public partial double DurationSeconds { get; set; } = 60.0;

    public double DurationMinutes
    {
        get => Math.Round(DurationSeconds / 60.0, 2);
        set
        {
            DurationSeconds = Math.Max(1.0, value * 60.0);
        }
    }

    [ObservableProperty]
    public partial bool EnableRest { get; set; }

    [ObservableProperty]
    public partial int RestSecondsAfter { get; set; } = 10;

    public string PaceFormatted => TargetSpeed <= 0
        ? "0:00 min/km"
        : FormatPace(TargetSpeed);

    private static string FormatPace(double speed)
    {
        double totalMinutes = 60.0 / speed;
        int minutes = (int)totalMinutes;
        int seconds = (int)((totalMinutes - minutes) * 60);

        return $"{minutes}:{seconds:D2} min/km";
    }
}

public partial class WorkoutPlan : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = AppResources.DefaultWorkoutTitle;

    [ObservableProperty]
    public partial ObservableCollection<WorkoutInterval> Intervals { get; set; } = [];
}