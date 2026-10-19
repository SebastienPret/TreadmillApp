# 🏃‍♂️ TreadmillApp - Contrôleur de Tapis de Course .NET MAUI

Application mobile et desktop développée en **.NET MAUI** permettant de piloter des tapis de course connectés compatibles avec le protocole Bluetooth standard **FTMS (Fitness Machine Service)**, de créer et gérer des programmes d'entraînement fractionnés (intervalles) personnalisés.

---

## 🚀 Fonctionnalités principales

1. **Tableau de Bord (Dashboard) :**
   * Pilotage en direct de la **vitesse** et de l'**inclinaison**.
   * Commandes **START**, **STOP** et **PAUSE** (manuelle ou intégrée).
   * Suivi en temps réel des données renvoyées par le tapis.

2. **Connexion Bluetooth BLE (FTMS) :**
   * Recherche et scan des appareils Bluetooth à proximité.
   * Filtre intelligent ciblant les équipements de fitness et les tapis compatibles.
   * Synchronisation des caractéristiques de contrôle (`0x2AD9`) et de données (`0x2ACD`).

3. **Créateur et Éditeur d'Intervalles :**
   * Configuration de blocs d'efforts sur-mesure (Nom, Vitesse cible, Inclinaison, Durée).
   * Option de **pause automatique de récupération** activable à la fin de chaque intervalle.
   * Suppression et modification dynamique des blocs existants.

4. **Gestion des Programmes d'Entraînement :**
   * Sauvegarde durable des plans d'entraînement sur l'appareil (via `Preferences` / JSON).
   * Page dédiée pour lister, lancer, modifier ou supprimer vos programmes enregistrés.

---

## 🛠️ Stack Technique

* **Framework :** .NET MAUI
* **Langage :** C#
* **Bluetooth :** Package NuGet `Plugin.BLE`
* **Architecture :** Modèle-Vue (MVVM / Services / Code-behind)

---

## 📦 Installation & Prérequis

### 1. Prérequis
* [.NET SDK](https://dotnet.microsoft.com/) (version 8.0 ou supérieure recommandée)
* **Visual Studio 2022** (avec la charge de travail *Développement d'interface utilisateur d'application multiplateforme .NET*) ou **VS Code** avec l'extension .NET MAUI.

### 2. Packages NuGet requis
Installez le package suivant dans votre projet :
```bash
dotnet add package Plugin.BLE

---

## 📁 Structure du Projet

TreadmillApp/
│
├── Models/
│   ├── WorkoutInterval.cs     # Modèle d'un intervalle et d'un plan d'entraînement
│
├── Resources/
│   ├── Strings/
│   │   ├── AppResources.resx    # Fichiers de ressources pour la localisation
│   │   ├── AppResources.fr-BE.resx # Traduction belge française
│   │   └── AppResources.nl-BE.resx # Traduction belge néerlandaise
│
├── Services/
│   ├── TreadmillService.cs    # Gestion du Bluetooth BLE (FTMS) et de la logique d'entraînement
│   └── WorkoutStorageService.cs # Persistance et sauvegarde des programmes
│
├── Views/
│   ├── DashboardPage.xaml     # Tableau de bord principal & contrôle direct
│   ├── ScanDevicesPage.xaml   # Page de recherche et sélection Bluetooth des tapis
│   ├── IntervalsPage.xaml     # Créateur / Éditeur d'intervalles
│   └── SavedWorkoutsPage.xaml # Liste des programmes enregistrés
│
├── AppShell.xaml              # Configuration de la navigation par onglets (TabBar)
└── App.xaml.cs                # Point d'entrée de l'application

---

## 📱 Utilisation

1. Lancez l'application sur votre appareil (Android, iOS ou Windows).
2. Rendez-vous sur l'onglet Tapis et cliquez sur "Rechercher un Tapis".
3. Sélectionnez votre tapis dans la liste des appareils FTMS détectés.
4. Dans l'onglet Programmes ou Créateur, configurez ou choisissez un entraînement en y ajoutant vos intervalles (avec ou sans pause automatique).
5. Lancez le plan depuis le tableau de bord et profitez de votre séance !


---

## Todo

+ Icon
+ Language
+ couleur
+ Recherhe tapis autre
+ ~~bouton inclinaison dynamique~~
+ selections d'intervale pour modification (rafrichissement)
+ connection avec MyWoosh
+ Settings page reload page
