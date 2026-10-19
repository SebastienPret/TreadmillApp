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
    }
}