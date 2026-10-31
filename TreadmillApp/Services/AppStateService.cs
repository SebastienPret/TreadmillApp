using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TreadmillApp.Models;

namespace TreadmillApp.Services
{
    public partial class AppStateService : ObservableObject
    {
        // 1. L'instance unique (le Singleton)
        public static AppStateService Instance { get; } = new AppStateService();

        // 2. Constructeur privé pour empêcher qu'on puisse l'instancier avec "new AppStateService()" ailleurs
        private AppStateService()
        {
            // Vous pouvez initialiser des choses ici si nécessaire
        }

        // 3. Vos variables globales avec notification automatique (grâce au CommunityToolkit)
        [ObservableProperty]
        public partial WorkoutPlan? CurrentPlan { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<WorkoutPlan> SavedWorkouts { get; set; } = [];

        // Méthode pratique pour charger ou recharger les entraînements globalement
        public void LoadWorkouts()
        {
            var workouts = WorkoutStorageService.LoadWorkouts();
            SavedWorkouts = new ObservableCollection<WorkoutPlan>(workouts ?? []);
        }
    }
}


/*
// Utilisation dans un ViewModel ou ailleurs :

// Pour définir ou lire le CurrentPlan
// Définir le plan actif (ex: avant de lancer ou modifier)
AppStateService.Instance.CurrentPlan = selectedPlan;

// Récupérer le plan actif
var plan = AppStateService.Instance.CurrentPlan;



// Pour utiliser la liste SavedWorkouts dans un ViewModel
// Récupérer la liste des entraînements sauvegardés
public ObservableCollection<WorkoutPlan> SavedWorkouts => AppStateService.Instance.SavedWorkouts;

// Et pour rafraîchir la liste globale après une modification ou une suppression :
AppStateService.Instance.LoadWorkouts();
*/
