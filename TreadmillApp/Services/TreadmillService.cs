using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.Maui.Audio;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TreadmillApp.Models;
using TreadmillApp.Resources.Strings;

namespace TreadmillApp.Services
{
    public class TreadmillService : INotifyPropertyChanged
    {
        private readonly IAdapter _adapter;
        private readonly IBluetoothLE _ble;
        private IDevice? _treadmillDevice;
        private ICharacteristic? _controlPointCharacteristic;
        private ICharacteristic? _treadmillDataCharacteristic;

        // L'UUID officiel du service FTMS (Fitness Machine Service)
        private static readonly Guid FtmsServiceUuid = Guid.Parse("00001826-0000-1000-8000-00805f9b34fb");
        private static readonly Guid TreadmillDataCharacteristicUuid = Guid.Parse("00002ACD-0000-1000-8000-00805f9b34fb");

        private double _currentSpeed;
        private double _currentIncline;
        private bool _isRunning;
        private bool _isConnected;
        private CancellationTokenSource? _workoutCancellationToken;

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); }
        }

        public double CurrentSpeed
        {
            get => _currentSpeed;
            set { _currentSpeed = value; OnPropertyChanged(); }
        }

        public double CurrentIncline
        {
            get => _currentIncline;
            set { _currentIncline = value; OnPropertyChanged(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        public ObservableCollection<IDevice> DiscoveredDevices { get; set; } = new();

        public event EventHandler<string>? IntervalStatusChanged;
        public event EventHandler<string>? DeviceStatusChanged;

        // Events to notify your ViewModel/UI
        public event EventHandler<double> SpeedUpdated;
        public event EventHandler TreadmillStarted;
        public event EventHandler TreadmillStopped;

        private bool _hasStartedReported = false;

        public TreadmillService()
        {
            _ble = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
        }

        public async Task StartScanningAsync()
        {
            // S'assure que le Bluetooth est allumé
            if (_ble.State != BluetoothState.On)
            {
                Debug.WriteLine("Le Bluetooth est désactivé.");
                await Application.Current!.Windows[0].Page.DisplayAlertAsync(AppResources.Error, AppResources.Bluetooth_Off, AppResources.Txt_OK);
                return;
            }
            DiscoveredDevices.Clear();
            _adapter.ScanTimeout = 10000; // 10 secondes

            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;

            // Définition des options de scan avec le filtre FTMS
            var scanOptions = new ScanFilterOptions
            {
                ServiceUuids = new[] { FtmsServiceUuid }
            };

            // Durée maximale du scan (ex: 10 secondes)
            _adapter.ScanTimeout = 10000;

            Debug.WriteLine("Début du scan des tapis FTMS...");

            // Lancement du scan avec le filtre d'UUID
            await _adapter.StartScanningForDevicesAsync(scanOptions);
        }

        private void OnDeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            if (e.Device == null || string.IsNullOrEmpty(e.Device.Name)) return;

            string name = e.Device.Name;
            //Collection<AdvertisementRecord> HUID = e.Device.AdvertisementRecords;
            Debug.WriteLine($"Appareil découvert : {name} ({e.Device.Id})");
            // Filtre par mots-clés typiques des tapis de course et équipements de fitness
            /*bool isTreadmill = name.Contains("Treadmill", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Fitness", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Run", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Domyos", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Nordictrack", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("SW7745EAI", StringComparison.OrdinalIgnoreCase);
            */
            if ( !DiscoveredDevices.Any(d => d.Id == e.Device.Id)) //isTreadmill &&
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DiscoveredDevices.Add(e.Device);
                });
            }
        }

        public async Task StopScanningAsync()
        {
            await _adapter.StopScanningForDevicesAsync();
            Debug.WriteLine("Scan arrêté.");
        }

        public async Task<bool> ConnectToDeviceAsync(IDevice device)
        {
            try
            {
                await StopScanningAsync();
                DeviceStatusChanged?.Invoke(this, $"{AppResources.DeviceStatus_Connecting} {device.Name}..."); // Connexion à

                await _adapter.ConnectToDeviceAsync(device);
                _treadmillDevice = device;
                IsConnected = true;

                var service = await _treadmillDevice.GetServiceAsync(Guid.Parse("00001826-0000-1000-8000-00805f9b34fb"));
                if (service != null)
                {
                    _controlPointCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("00002ad9-0000-1000-8000-00805f9b34fb"));
                    _treadmillDataCharacteristic = await service.GetCharacteristicAsync(Guid.Parse("00002acd-0000-1000-8000-00805f9b34fb"));

                    if (_treadmillDataCharacteristic != null)
                    {
                        _treadmillDataCharacteristic.ValueUpdated += (s, e) => ParseTreadmillData(e.Characteristic.Value);
                        await _treadmillDataCharacteristic.StartUpdatesAsync();
                    }
                }

                DeviceStatusChanged?.Invoke(this, AppResources.DeviceStatus_Connected); // Connecté au Tapis !
                return true;
            }
            catch (Exception ex)
            {
                DeviceStatusChanged?.Invoke(this, $"{AppResources.Error} : {ex.Message}"); // Erreur
                return false;
            }
        }

        public async Task StartAsync()
        {
            IsRunning = true;
            if (_controlPointCharacteristic != null)
            {
                await _controlPointCharacteristic.WriteAsync(new byte[] { 0x00 });
                await _controlPointCharacteristic.WriteAsync(new byte[] { 0x07, 0x01 });
            }
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            IsRunning = false;
            _workoutCancellationToken?.Cancel();
            CurrentSpeed = 0;

            if (_controlPointCharacteristic != null)
            {
                await _controlPointCharacteristic.WriteAsync(new byte[] { 0x08, 0x01 });
            }
            await Task.CompletedTask;
        }

        public async Task SetSpeedAsync(double speedKmH)
        {
            CurrentSpeed = Math.Max(0, Math.Min(22, speedKmH));

            if (_controlPointCharacteristic != null && IsRunning)
            {
                ushort speedValue = (ushort)(CurrentSpeed * 100);
                byte[] data = new byte[] { 0x02, (byte)(speedValue & 0xFF), (byte)(speedValue >> 8) };
                await _controlPointCharacteristic.WriteAsync(data);
            }
        }

        public async Task SetInclineAsync(double inclinePercentage)
        {
            CurrentIncline = Math.Max(0, Math.Min(15, inclinePercentage));

            if (_controlPointCharacteristic != null && IsRunning)
            {
                short inclineValue = (short)(CurrentIncline * 10);
                byte[] data = new byte[] { 0x03, (byte)(inclineValue & 0xFF), (byte)(inclineValue >> 8) };
                await _controlPointCharacteristic.WriteAsync(data);
            }
        }

        private void ParseTreadmillData(byte[] data)
        {
            if (data == null || data.Length < 4) return;

            // Parse Instantaneous Speed (Standard FTMS specification)
            // Bytes 0-1: Flags, Bytes 2-3: Instantaneous Speed (in 0.01 km/h)
            ushort speedRaw = (ushort)(data[2] | (data[3] << 8));
            CurrentSpeed = speedRaw / 100.0;

            // Notify listeners of the current speed
            SpeedUpdated?.Invoke(this, CurrentSpeed);

            // Detect if the treadmill has started (Speed > 0.5 km/h)
            if (CurrentSpeed > 0.5 && !_hasStartedReported)
            {
                _hasStartedReported = true;
                TreadmillStarted?.Invoke(this, EventArgs.Empty);
                System.Diagnostics.Debug.WriteLine("Treadmill movement detected: STARTED");
            }
            // Detect if the treadmill has stopped (Speed drops back to 0)
            else if (CurrentSpeed <= 0.1 && _hasStartedReported)
            {
                _hasStartedReported = false;
                TreadmillStopped?.Invoke(this, EventArgs.Empty);
                System.Diagnostics.Debug.WriteLine("Treadmill movement detected: STOPPED");
            }
        }

        public async Task RunWorkoutPlanAsync(Models.WorkoutPlan plan, Action<string, int> onProgress)
        {
            
            _workoutCancellationToken = new CancellationTokenSource();
            bool IsPaused = false; // pause pas implementée dans cette version, mais peut être ajoutée avec un bouton de pause dans l'UI
            await StartAsync();
            IntervalStatusChanged?.Invoke(this, AppResources.IntervalStatus_Starting_Workout + " [" + plan.Title + "]"); // Démarrage de l'entraînement...
                                                                                               // waiting for the treadmill to be ready (optional, depending on treadmill behavior)
                                                                                               // retrieve the current state of the treadmill and wait until it is ready to accept commands

            // 1. Check if the treadmill is already moving
            while (!_hasStartedReported && !_workoutCancellationToken.Token.IsCancellationRequested)
            {
                Debug.WriteLine("Waiting for the user to start the treadmill...");
                await Task.Delay(500, _workoutCancellationToken.Token);
            }

            Debug.WriteLine("Treadmill is running! Starting workout plan...");

            while (_hasStartedReported == true || !Debugger.IsAttached)
            {
                // await Task.Delay(500, _workoutCancellationToken.Token);
            
#if DEBUG
                Debug.WriteLine("Treadmill is not ready yet. Waiting for it to be ready...");
                await Task.Delay(5000, _workoutCancellationToken.Token); // simulate a delay for treadmill readiness in debug mode
                Debug.WriteLine("Treadmill is ready for workout plan execution.");
            
#endif
                // set a 1km/h speed limit for safety and wait 5 seconds before starting the interval
                //await SetSpeedAsync(1);
                //await Task.Delay(5000, _workoutCancellationToken.Token);

                try
                {
                    // activer les alertes sonores si activées dans les paramètres
                    if (AppSettingsService.Instance.EnableAudioAlerts == true)
                    {
                       await PlayBeepSoundAsync();
                    }

                    foreach (var interval in plan.Intervals)
                    {
                    

                        if (_workoutCancellationToken.Token.IsCancellationRequested) break;

                        // 5. Exécuter l'intervalle avec un compte à rebours de 5 secondes avant le début
                        RunIntervalAsync(interval, _workoutCancellationToken.Token);

                    
                    
                        // 1. COMPTE À REBOURS DE 5s (Si activé dans les Paramètres)
                        if (AppSettingsService.Instance.EnableCountdown)
                        {
                        
                            // Optionnel : Mettre une vitesse de préparation douce ou garder le tapis stable
                            for (int countdown = 4; countdown > 0; countdown--)
                            {
                                if (_workoutCancellationToken.Token.IsCancellationRequested) break;

                                // Gérer la pause manuelle pendant le compte à rebours
                                while (IsPaused)
                                {
                                    if (_workoutCancellationToken.Token.IsCancellationRequested) break;
                                    await Task.Delay(1000, _workoutCancellationToken.Token);
                                }

                                // Joue un bip à chaque seconde du décompte (ou un bip plus grave/aigu sur la dernière seconde)
                                // await PlayBeepSoundAsync();

                                // Notifie l'UI du compte à rebours (ex: "Prêt pour [Nom] dans 5s...")
                                IntervalStatusChanged?.Invoke(this, $"{AppResources.Txt_Ready_For} [{interval.Name}] {AppResources.Txt_In} {countdown}s..."); // Prêt pour [{interval.Name}] dans {countdown}s...
                                await Task.Delay(1000, _workoutCancellationToken.Token);
                            }
                        }

                        if (_workoutCancellationToken.Token.IsCancellationRequested) break;

                        // 2. Envoyer les consignes de l'intervalle (Vitesse & Inclinaison)
                        await SetSpeedAsync(interval.TargetSpeed);
                        await SetInclineAsync(interval.TargetIncline);

                        // 3. Exécution de l'intervalle (Effort)
                        //await PlayBeepSoundAsync(); // Ou lors du passage à la phase d'effort / de pause :
                        IntervalStatusChanged?.Invoke(this, $"{AppResources.IntervalStatus_Starting} : {interval.Name}"); //Phase

                        for (double remaining = interval.DurationSeconds; remaining > 0; remaining--)
                        {
                            while (IsPaused)
                            {
                                if (_workoutCancellationToken.Token.IsCancellationRequested) break;
                                await Task.Delay(500, _workoutCancellationToken.Token);
                            }
                            onProgress(interval.Name, Convert.ToInt32(remaining));
                            await Task.Delay(1000, _workoutCancellationToken.Token);
                        }

                        // 4. PAUSE AUTOMATIQUE (Si activée pour cet intervalle)
                        if (interval.EnableRest && interval.RestSecondsAfter > 0)
                        {
                            await SetSpeedAsync(4.0); // Vitesse lente de récupération
                            await SetInclineAsync(0);

                            for (int restRemaining = interval.RestSecondsAfter; restRemaining > 0; restRemaining--)
                            {
                                if (_workoutCancellationToken.Token.IsCancellationRequested) break;

                                while (IsPaused)
                                {
                                    if (_workoutCancellationToken.Token.IsCancellationRequested) break;
                                    await Task.Delay(500, _workoutCancellationToken.Token);
                                }

                                IntervalStatusChanged?.Invoke(this, $"{AppResources.Txt_Rest_Pause} : {restRemaining}s"); // Pause automatique
                                await Task.Delay(1000, _workoutCancellationToken.Token);
                            }
                        }
                    }
                }
                catch (TaskCanceledException) { }
                finally
                {
                    IsPaused = false;
                    await StopAsync();
                    IntervalStatusChanged?.Invoke(this, AppResources.IntervalStatus_Completed); // Entraînement terminé !
                }
            }
        }


        public async static void RunIntervalAsync(WorkoutInterval interval, CancellationToken cancellationToken)
        {
            int totalSeconds = (int)interval.DurationSeconds;
            bool fiveSecondAlertPlayed = false;

            for (int elapsed = 0; elapsed < totalSeconds; elapsed++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                int remainingSeconds = totalSeconds - elapsed;

                // Check if we are exactly 5 seconds before the end
                if (remainingSeconds == 5 && !fiveSecondAlertPlayed)
                {
                    PlayBeepSoundAsync();
                    fiveSecondAlertPlayed = true; // Ensures it only plays once for this interval
                }

                // Wait 1 second for the interval progress
                await Task.Delay(1000, cancellationToken);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private async static Task PlayBeepSoundAsync()
        {
            // Vérifie si les alertes sonores sont activées dans les paramètres
            if (!AppSettingsService.Instance.EnableAudioAlerts) return;

            try
            {
                // Ouvre le fichier audio depuis le dossier Resources/Raw
                var audioStream = await FileSystem.OpenAppPackageFileAsync("mariostart.mp3");
                var player = AudioManager.Current.CreatePlayer(audioStream);
                player.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lecture audio : {ex.Message}");
            }
        }

        public  string PaceFormattedString(string speedString)
        {
            if (double.TryParse(speedString, out double speed) && speed > 0)
            {
                // Calcul du temps en minutes pour 1 km
                double totalMinutes = 60.0 / speed;
                int minutes = (int)totalMinutes;
                int seconds = (int)((totalMinutes - minutes) * 60);
                return $"{minutes}:{seconds:D2}";
            }
            return "0:00";
        }

        
    }
}