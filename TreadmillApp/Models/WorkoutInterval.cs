using System.ComponentModel;
using TreadmillApp.Resources.Strings;
using System.Runtime.CompilerServices;
// Models/WorkoutInterval.cs

namespace TreadmillApp.Models
{
    public class WorkoutInterval : INotifyPropertyChanged
    {
        public string Name { get; set; } = AppResources.DefaultIntervalName; // ex: "Sprint", "Récupération", "Effort"
        public double TargetSpeed { get; set; } = 10.0; // km/h
        public double TargetIncline { get; set; } = 1.0; // %
        //public int DurationSeconds { get; set; } = 60; // Durée en secondes
        private double _durationSeconds = 60.0;
        private double _durationMinutes = 1.0;
        // 1er champ : La valeur en Secondes
        public double DurationSeconds
        {
            get => _durationSeconds;
            set
            {
                // Empêche les valeurs nulles, négatives ou égales à 0 (minimum 1 seconde)
                double safeValue = (value <= 0) ? 1.0 : value;

                if (Math.Abs(_durationSeconds - safeValue) > 0.001)
                {
                    _durationSeconds = safeValue;
                    OnPropertyChanged(); // Notifie le 1er Entry
                    OnPropertyChanged(nameof(DurationMinutes)); // Notifie le 2ème Entry (valeur / 60.0)
                }
            }
        }

        // 2ème champ : La valeur en Minutes (valeur du premier / 60.0)
        public double DurationMinutes
        {
            get => Math.Round(_durationSeconds / 60.0, 2);
            set
            {
                // Si l'utilisateur modifie directement les minutes, on recalcule les secondes
                double newSeconds = value * 60.0;
                double safeSeconds = Math.Max(1.0, newSeconds);
                if (Math.Abs(_durationSeconds - safeSeconds) > 0.001)
                {
                    //_durationSeconds = safeSeconds;
                    _durationSeconds = safeSeconds;
                    OnPropertyChanged(nameof(DurationSeconds)); // Notifie le 1er Entry
                    OnPropertyChanged(); // Notifie le 2ème Entry
                }
            }
        }
        //public double DurationMinutes { get; set; } = 1;

        // Propriété intermédiaire pour l'affichage et la saisie en Minutes
        /*public double DurationMinutes
        {
            get => Math.Round(DurationSeconds / 60.0, 2); // Convertit les secondes en minutes
            set => DurationSeconds = (int)(value * 60); // Convertit les minutes saisies en secondes
        }*/

        // Propriétés pour la pause automatique
        public bool EnableRest { get; set; } = false;
        public int RestSecondsAfter { get; set; } = 10; // Durée de la pause si activée

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class WorkoutPlan
    {
        public string Title { get; set; } = AppResources.DefaultWorkoutTitle; //"Mon Entraînement"
        public List<WorkoutInterval> Intervals { get; set; } = new();
            
        }
    

    }