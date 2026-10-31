using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TreadmillApp.Models;
using TreadmillApp.Resources.Strings;
using TreadmillApp.Services;
using TreadmillApp.Views;

namespace TreadmillApp.ViewModels
{
    public partial class IntervalsViewModel : ObservableObject
    {

        //public static WorkoutPlan CurrentPlan => IntervalsPage.CurrentPlan;
        [ObservableProperty]
        public partial WorkoutPlan CurrentPlan { get; set; } = new();

        [ObservableProperty]
        public partial string PlanTitle { get; set; } = CurrentPlan.Title ?? AppResources.DefaultWorkoutTitle;

        // Propriétés du formulaire d'intervalle en cours de création
        [ObservableProperty]
        public partial string IntervalName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial double Speed { get; set; } = 0.0;

        [ObservableProperty]
        public partial string PaceFormatted { get; set; } = "0:00 min/km";

        [ObservableProperty]
        public partial double Incline { get; set; } = 0;

        [ObservableProperty]
        public partial double DurationMinutes { get; set; } = 0;

        [ObservableProperty]
        public partial double DurationSeconds { get; set; } = 0;

        // Collection observable pour la liste d'intervalles
        //public static ObservableCollection<WorkoutInterval> Intervals => CurrentPlan.Intervals;

        public ObservableCollection<WorkoutInterval> Intervals { get; }

        public IntervalsViewModel()
        {
            // Convertit la List<WorkoutInterval> en ObservableCollection<WorkoutInterval>
            Intervals = new ObservableCollection<WorkoutInterval>(CurrentPlan.Intervals);
        }

        partial void OnSpeedChanged(double value)
        {
            UpdatePace(value);
        }

        partial void OnDurationMinutesChanged(double value)
        {
            DurationSeconds = value * 60;
            OnPropertyChanged(nameof(DurationSeconds));
        }

        partial void OnDurationSecondsChanged(double value)
        {
            DurationMinutes = Math.Round(value / 60.0, 2);
            OnPropertyChanged(nameof(DurationMinutes));
        }

        private void UpdatePace(double speed)
        {
            if (speed <= 0)
            {
                PaceFormatted = "0:00 min/km";
                return;
            }

            double totalMinutes = 60.0 / speed;
            int minutes = (int)totalMinutes;
            int seconds = (int)((totalMinutes - minutes) * 60);

            PaceFormatted = $"{minutes}:{seconds:D2} min/km";
        }

        [RelayCommand]
        private async Task AddInterval()
        {
            if (DurationSeconds <= 0) // Speed <= 0 ||
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Error, AppResources.Error_Invalid_Interval, AppResources.Txt_OK); // durée invalide
                return;
            }

            var interval = new WorkoutInterval
            {
                Name = string.IsNullOrWhiteSpace(IntervalName) ? AppResources.DefaultIntervalName : IntervalName,
                TargetSpeed = Speed,
                PaceFormattedString = PaceFormatted,
                
                TargetIncline = Incline,
                DurationSeconds = DurationSeconds
            };

            CurrentPlan.Intervals.Add(interval);
            Intervals.Add(interval);

            // Réinitialisation du formulaire
            //IntervalName = string.Empty;
        }

        [RelayCommand]
        private void DeleteInterval(WorkoutInterval interval)
        {
            if (interval != null && CurrentPlan.Intervals.Contains(interval))
            {
                CurrentPlan.Intervals.Remove(interval);
                Intervals.Remove(interval);
            }
        }

        [RelayCommand]
        private async Task SaveWorkout()
        {
            if (CurrentPlan.Intervals.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Error, AppResources.Error_Add_Interval, AppResources.Txt_OK);
                return;
            }

            CurrentPlan.Title = string.IsNullOrWhiteSpace(PlanTitle) ? AppResources.DefaultWorkoutTitle : PlanTitle;
            // Définir le plan actif 
            AppStateService.Instance.CurrentPlan = CurrentPlan;
            WorkoutStorageService.SaveWorkout(CurrentPlan);

            await Shell.Current.DisplayAlertAsync(AppResources.Txt_Success, AppResources.Success_Save_Workout, AppResources.Txt_OK);
        }

        [RelayCommand]
        private async Task StartWorkout()
        {
            if (CurrentPlan.Intervals.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Error, AppResources.Error_Add_Interval, AppResources.Txt_OK);
                return;
            }

            // Définir le plan actif 
            AppStateService.Instance.CurrentPlan = CurrentPlan;
            CurrentPlan.Title = string.IsNullOrWhiteSpace(PlanTitle) ? AppResources.DefaultWorkoutTitle : PlanTitle;
            await Shell.Current.GoToAsync("//DashboardPage");
        }
    }
}