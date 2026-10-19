using System.Diagnostics;
using System.Globalization;
using TreadmillApp.Models;
using TreadmillApp.Resources.Strings;
using TreadmillApp.Services;

namespace TreadmillApp.Views
{
    public partial class IntervalsPage : ContentPage
    {
        public static WorkoutPlan CurrentPlan { get; set; } = new();

        public IntervalsPage()
        {
            InitializeComponent();
            CvIntervals.ItemsSource = CurrentPlan.Intervals;
            TxtPlanTitle.Text = CurrentPlan.Title;
            BindingContext = new Models.WorkoutInterval();
        }

        private void StpSpeed_ValueChanged(object sender, ValueChangedEventArgs e) => LblSpeed.Text = e.NewValue.ToString("0.0");
        private void StpIncline_ValueChanged(object sender, ValueChangedEventArgs e) => LblIncline.Text = e.NewValue.ToString("0");

        private void OnAddIntervalClicked(object sender, EventArgs e)
        {
            if (double.TryParse(LblSpeed.Text, out double speed) && int.TryParse(TxtDuration.Text, out int duration))
            {
                var interval = new WorkoutInterval
                {
                    Name = string.IsNullOrWhiteSpace(TxtName.Text) ? AppResources.DefaultIntervalName : TxtName.Text, //Effort
                    TargetSpeed = speed,
                    TargetIncline = double.Parse(LblIncline.Text),
                    DurationSeconds = duration
                };

                CurrentPlan.Intervals.Add(interval);
                CvIntervals.ItemsSource = null;
                CvIntervals.ItemsSource = CurrentPlan.Intervals;
            }
        }
        private async void OnSaveWorkoutClicked(object sender, EventArgs e)
        {
            if (CurrentPlan.Intervals.Count == 0)
            {
                await DisplayAlertAsync(AppResources.Error, AppResources.Error_Add_Interval, AppResources.Txt_OK); //"Erreur", "Ajoutez au moins un intervalle avant de sauvegarder.", "OK"
                return;
            }

            CurrentPlan.Title = string.IsNullOrWhiteSpace(TxtPlanTitle.Text) ? AppResources.DefaultWorkoutTitle : TxtPlanTitle.Text; //"Entraînement Sans Nom"
            WorkoutStorageService.SaveWorkout(CurrentPlan);

            await DisplayAlertAsync(AppResources.Txt_Success, AppResources.Success_Save_Workout, AppResources.Txt_OK); //"Succès", "L'entraînement a été sauvegardé avec succès !", "OK"
        }
        private async void OnStartWorkoutClicked(object sender, EventArgs e)
        {
            if (CurrentPlan.Intervals.Count == 0)
            {
                await DisplayAlertAsync(AppResources.Error, AppResources.Error_Add_Interval, AppResources.Txt_OK); //"Erreur", "Veuillez ajouter au moins un intervalle.", "OK"
                return;
            }
            CurrentPlan.Title = string.IsNullOrWhiteSpace(TxtPlanTitle.Text) ? AppResources.DefaultWorkoutTitle : TxtPlanTitle.Text; //"Entraînement Sans Nom"
            await Shell.Current.GoToAsync("//DashboardPage");
        }

        private void OnDeleteIntervalClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is WorkoutInterval interval)
            {
                CurrentPlan.Intervals.Remove(interval);
                CvIntervals.ItemsSource = null;
                CvIntervals.ItemsSource = CurrentPlan.Intervals;
            }
        }

        private void OnDurationSecondsChanged(object sender, TextChangedEventArgs e)
        {
            int duration = e.NewTextValue != null && int.TryParse(e.NewTextValue, out int d) ? d : 0;
            Debug.WriteLine($"Duration text changed: {duration}");
            // Math.Round(DurationSeconds / 60.0, 2); // Convertit les secondes en minutes
            // set => DurationSeconds = (int)(value * 60); // Convertit les minutes saisies en secondes
            if (string.IsNullOrEmpty(e.NewTextValue))
            {
                Debug.WriteLine("Duration text is empty.");
            }
            else
            {
                
                Debug.WriteLine($"Duration text is: {Math.Round(duration / 60.0, 2)}");
                TxtDurationMinutes.Text = Math.Round(duration / 60.0, 2).ToString(); ; // Update the TxtDurationMinutes property with the new text value
            }
        }
        private void OnDurationMinutesChanged(object sender, TextChangedEventArgs e)
        {
            int duration = e.NewTextValue != null && int.TryParse(e.NewTextValue, out int d) ? d : 0;
            Debug.WriteLine($"Duration text changed: {duration}");
            // Math.Round(DurationSeconds / 60.0, 2); // Convertit les secondes en minutes
            // set => DurationSeconds = (int)(value * 60); // Convertit les minutes saisies en secondes
            if (string.IsNullOrEmpty(e.NewTextValue))
            {
                Debug.WriteLine("Duration text is empty.");
            }
            else
            {
                
                Debug.WriteLine($"Duration text is: {duration * 60.0}");
                TxtDurationMinutes.Text = (duration * 60.0).ToString(); ; // Update the TxtDurationMinutes property with the new text value
            }
        }

        // Quand l'utilisateur retape dans le champ Secondes
        private void OnSecondsTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry && entry.BindingContext is Models.WorkoutInterval interval)
            {
                // PROTECTION : Si le champ est vide ou effacé par l'utilisateur
                if (string.IsNullOrWhiteSpace(e.NewTextValue))
                {
                    interval.DurationSeconds = 1; // Valeur par défaut sécurisée
                    return;
                }

                string text = e.NewTextValue.Replace(',', '.');
                if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double sec))
                {
                    // Protection supplémentaire : si la valeur saisie est 0
                    interval.DurationSeconds = sec <= 0 ? 1 : sec; // La propriété calculera automatiquement min = sec / 60.0
                }
            }
        }

        // Quand l'utilisateur retape dans le champ Minutes
        private void OnMinutesTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry && entry.BindingContext is Models.WorkoutInterval interval)
            {
                if (string.IsNullOrWhiteSpace(e.NewTextValue))
                {
                    interval.DurationMinutes = 0;
                    return;
                }

                string text = e.NewTextValue.Replace(',', '.');
                if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double min))
                {
                    interval.DurationMinutes = min; // La propriété calculera automatiquement sec = min * 60.0
                }
            }
        }
    }
}