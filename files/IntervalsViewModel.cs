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
        
        //[ObservableProperty]
        //public partial string PlanTitle { get; set; } = CurrentPlan.Title ?? AppResources.DefaultWorkoutTitle;
        [ObservableProperty]
        public partial string PlanTitle { get; set; } = AppStateService.Instance.CurrentPlan?.Title ?? AppResources.DefaultWorkoutTitle;

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
        public partial double Resistance { get; set; } = 0;

        // Selon le type d'appareil, le formulaire affiche vitesse/pente (tapis) ou résistance (elliptique)
        public bool IsTreadmill => DashboardPage.Treadmill.MachineType == MachineType.Treadmill;
        public bool IsCrossTrainer => DashboardPage.Treadmill.MachineType == MachineType.CrossTrainer;

        [ObservableProperty]
        public partial double DurationMinutes { get; set; } = 0;

        [ObservableProperty]
        public partial double DurationSeconds { get; set; } = 0;

        // Collection observable pour la liste d'intervalles
        //public static ObservableCollection<WorkoutInterval> Intervals => AppStateService.Instance.CurrentPlan?.Intervals ?? [];

        // Propriété non-statique exposant directement la collection du plan global
        /*public ObservableCollection<WorkoutInterval> Intervals { get; set; } =
            AppStateService.Instance.CurrentPlan?.Intervals ?? [];
        */

        // Remplace l'ancienne déclaration par celle-ci :
        [ObservableProperty]
        public partial ObservableCollection<WorkoutInterval> Intervals { get; set; } = [];

        // Intervalle en cours de modification (null = mode ajout)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditing))]
        [NotifyPropertyChangedFor(nameof(AddButtonText))]
        public partial WorkoutInterval? EditingInterval { get; set; }

        public bool IsEditing => EditingInterval != null;

        public string AddButtonText => IsEditing
            ? AppResources.Btn_Update_Interval
            : AppResources.Btn_Add_Interval;

        public IntervalsViewModel()
        {
            // Convertit la List<WorkoutInterval> en ObservableCollection<WorkoutInterval>
            //Intervals = new ObservableCollection<WorkoutInterval>(AppStateService.Instance.CurrentPlan?.Intervals);
            //Intervals = AppStateService.Instance.CurrentPlan?.Intervals ?? [];
            
            // Sécurité : si aucun plan n'a été initialisé, on en crée un par défaut
            /*if (AppStateService.Instance.CurrentPlan == null)
            {
                AppStateService.Instance.CurrentPlan = new WorkoutPlan
                {
                    Title = AppResources.DefaultWorkoutTitle,
                    Intervals = []
                };
            }*/
            LoadCurrentPlan();
        }

        public void LoadCurrentPlan()
        {
            var plan = AppStateService.Instance.CurrentPlan;

            if (plan == null)
            {
                // Crée un plan par défaut si aucun n'existe
                plan = new WorkoutPlan
                {
                    Title = AppResources.DefaultWorkoutTitle,
                    Intervals = []
                };
                AppStateService.Instance.CurrentPlan = plan;
            }
           

            PlanTitle = plan.Title ?? AppResources.DefaultWorkoutTitle;

            // Assure-toi que la collection pointe bien vers celle du plan actif
            plan.Intervals ??= [];

            CurrentPlan = plan;                 // garde le VM synchronisé avec le plan réel
            PlanTitle = plan.Title ?? AppResources.DefaultWorkoutTitle;
            OnPropertyChanged(nameof(IsTreadmill));      // le type d'appareil a pu changer depuis la dernière visite
            OnPropertyChanged(nameof(IsCrossTrainer));
            Intervals = plan.Intervals;         // même référence : plus de copie, plus de désynchronisation

        }
        

        partial void OnSpeedChanged(double value)
        {
            UpdatePace(value);
        }

        private bool _isSyncingDuration;

        partial void OnDurationMinutesChanged(double value)
        {
            //DurationSeconds = value * 60;
            //OnPropertyChanged(nameof(DurationSeconds));
            if (_isSyncingDuration) return;

            _isSyncingDuration = true;
            try
            {
                DurationSeconds = Math.Round(value * 60);
            }
            finally
            {
                _isSyncingDuration = false;
            }
        }

        

        partial void OnDurationSecondsChanged(double value)
        {
            //DurationMinutes = Math.Round(value / 60.0, 2);
            //OnPropertyChanged(nameof(DurationMinutes));
            if (_isSyncingDuration) return;

            _isSyncingDuration = true;
            try
            {
                DurationMinutes = Math.Round(value / 60.0, 2);
            }
            finally
            {
                _isSyncingDuration = false;
            }
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

            var plan = AppStateService.Instance.CurrentPlan;
            if (plan == null) return;

            var interval = new WorkoutInterval
            {
                Name = string.IsNullOrWhiteSpace(IntervalName) ? AppResources.DefaultIntervalName : IntervalName,
                TargetSpeed = Speed,
                PaceFormattedString = PaceFormatted,
                TargetIncline = Incline,
                TargetResistance = Resistance,
                DurationSeconds = DurationSeconds
            };

            if (EditingInterval != null)
            {
                int index = plan.Intervals.IndexOf(EditingInterval);
                if (index >= 0)
                    plan.Intervals[index] = interval;   // remplace sur place, les suivants restent intacts

                CancelEdit();
            }
            else
            {
                plan.Intervals.Add(interval);
            }
        }

        [RelayCommand]
        private void EditInterval(WorkoutInterval interval)
        {
            if (interval == null) return;

            EditingInterval = interval;
            IntervalName = interval.Name;
            Speed = interval.TargetSpeed;
            Incline = interval.TargetIncline;
            Resistance = interval.TargetResistance;
            DurationSeconds = interval.DurationSeconds;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            EditingInterval = null;
            IntervalName = string.Empty;
            // optionnel : remettre Speed / Incline / Durée à 0
        }

        [RelayCommand]
        private void DeleteInterval(WorkoutInterval interval)
        {
            var plan = AppStateService.Instance.CurrentPlan;
            if (interval != null && plan?.Intervals.Contains(interval) == true)
            {
                if (interval == EditingInterval) CancelEdit();
                plan.Intervals.Remove(interval);
            }
        }

        [RelayCommand]
        private async Task SaveWorkout()
        {
            var currentPlan = AppStateService.Instance.CurrentPlan;
            if (currentPlan?.Intervals.Count == 0 || currentPlan == null)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Error, AppResources.Error_Add_Interval, AppResources.Txt_OK);
                return;
            }

            currentPlan.Title = string.IsNullOrWhiteSpace(PlanTitle) ? AppResources.DefaultWorkoutTitle : PlanTitle;
            // Définir le plan actif 
            //AppStateService.Instance.CurrentPlan = AppStateService.Instance.CurrentPlan;
            WorkoutStorageService.SaveWorkout(currentPlan);

            await Shell.Current.DisplayAlertAsync(AppResources.Txt_Success, AppResources.Success_Save_Workout, AppResources.Txt_OK);
        }

        [RelayCommand]
        private async Task StartWorkout()
        {
            var currentPlan = AppStateService.Instance.CurrentPlan;
            if (currentPlan?.Intervals.Count == 0 || currentPlan == null)
            {
                await Shell.Current.DisplayAlertAsync(AppResources.Error, AppResources.Error_Add_Interval, AppResources.Txt_OK);
                return;
            }

            // Définir le plan actif 
            //AppStateService.Instance.CurrentPlan = AppStateService.Instance.CurrentPlan;
            currentPlan?.Title = string.IsNullOrWhiteSpace(PlanTitle) ? AppResources.DefaultWorkoutTitle : PlanTitle;
            AppStateService.Instance.AutoStartWorkout = true;   // demande le lancement automatique
            await Shell.Current.GoToAsync("//DashboardPage");
        }
    }
}