using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TreadmillApp.Resources.Strings;
using Plugin.Maui.Audio;

namespace TreadmillApp.Services
{
    public class TreadmillService : INotifyPropertyChanged
    {
        private readonly IAdapter _adapter;
        private readonly IBluetoothLE _ble;
        private IDevice? _treadmillDevice;
        private ICharacteristic? _controlPointCharacteristic;
        private ICharacteristic? _treadmillDataCharacteristic;

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

        public TreadmillService()
        {
            _ble = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
        }

        public async Task StartScanningAsync()
        {
            DiscoveredDevices.Clear();
            _adapter.ScanTimeout = 10000; // 10 secondes

            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;

            await _adapter.StartScanningForDevicesAsync();
        }

        private void OnDeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            if (e.Device == null || string.IsNullOrEmpty(e.Device.Name)) return;

            string name = e.Device.Name;
            Debug.WriteLine($"Appareil découvert : {name} ({e.Device.Id})");
            // Filtre par mots-clés typiques des tapis de course et équipements de fitness
            bool isTreadmill = name.Contains("Treadmill", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Fitness", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Run", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Domyos", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Nordictrack", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("SW7745EAI", StringComparison.OrdinalIgnoreCase);

            if (isTreadmill && !DiscoveredDevices.Any(d => d.Id == e.Device.Id))
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
            ushort speedRaw = (ushort)(data[2] | (data[3] << 8));
            CurrentSpeed = speedRaw / 100.0;
            /*if (data.Length >= 6)
            {
                short inclineRaw = (short)(data[4] | (data[5] << 8));
                CurrentIncline = inclineRaw / 10.0;
            }*/
        }

        public async Task RunWorkoutPlanAsync(Models.WorkoutPlan plan, Action<string, int> onProgress)
        {
            _workoutCancellationToken = new CancellationTokenSource();
            bool IsPaused = false; // pause pas implementée dans cette version, mais peut être ajoutée avec un bouton de pause dans l'UI
            await StartAsync();

            // set a 1km/h speed limit for safety and wait 5 seconds before starting the interval
            //await SetSpeedAsync(1);
            await Task.Delay(5000, _workoutCancellationToken.Token);

            try
            {
                foreach (var interval in plan.Intervals)
                {
                    if (_workoutCancellationToken.Token.IsCancellationRequested) break;

                    // 1. COMPTE À REBOURS DE 5s (Si activé dans les Paramètres)
                    if (AppSettingsService.Instance.EnableCountdown)
                    {
                        PlayBeepSoundAsync();
                        // Optionnel : Mettre une vitesse de préparation douce ou garder le tapis stable
                        for (int countdown = 4; countdown > 0; countdown--)
                        {
                            if (_workoutCancellationToken.Token.IsCancellationRequested) break;

                            // Gérer la pause manuelle pendant le compte à rebours
                            while (IsPaused)
                            {
                                if (_workoutCancellationToken.Token.IsCancellationRequested) break;
                                await Task.Delay(500, _workoutCancellationToken.Token);
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

                    for (int remaining = interval.DurationSeconds; remaining > 0; remaining--)
                    {
                        while (IsPaused)
                        {
                            if (_workoutCancellationToken.Token.IsCancellationRequested) break;
                            await Task.Delay(500, _workoutCancellationToken.Token);
                        }
                        onProgress(interval.Name, remaining);
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private async Task PlayBeepSoundAsync()
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
    }
}