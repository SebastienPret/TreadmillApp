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
        private ICharacteristic? _dataCharacteristic;

        // L'UUID officiel du service FTMS (Fitness Machine Service)
        private static readonly Guid FtmsServiceUuid = Guid.Parse("00001826-0000-1000-8000-00805f9b34fb");
        private static readonly Guid TreadmillDataCharacteristicUuid = Guid.Parse("00002ACD-0000-1000-8000-00805f9b34fb");
        private static readonly Guid CrossTrainerDataCharacteristicUuid = Guid.Parse("00002ACE-0000-1000-8000-00805f9b34fb");
        private static readonly Guid ResistanceRangeCharacteristicUuid = Guid.Parse("00002AD6-0000-1000-8000-00805f9b34fb");

        private const double RestSpeedKmH = 4.0;   // vitesse de récupération
        private const double RestResistanceLevel = 1.0;   // résistance de récupération (elliptique)
        private const int CountdownSeconds = 4;    // compte à rebours avant chaque intervalle

        private bool _isWorkoutRunning;
        private bool _isPaused;

        private double _currentSpeed;
        private double _currentIncline;
        private bool _isRunning;
        private bool _isConnected;
        private CancellationTokenSource? _workoutCancellationToken;

        public bool IsPaused
        {
            get => _isPaused;
            set { _isPaused = value; OnPropertyChanged(); }
        }

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

        // ---------- Type d'appareil (tapis / elliptique) ----------
        private const string MachineTypePrefKey = "MachineType";
        private MachineType _machineType = (MachineType)Preferences.Default.Get(MachineTypePrefKey, (int)MachineType.Treadmill);

        /// <summary>Type d'appareil. Mémorisé, et mis à jour automatiquement à la connexion.</summary>
        public MachineType MachineType
        {
            get => _machineType;
            set
            {
                if (_machineType == value) return;
                _machineType = value;
                Preferences.Default.Set(MachineTypePrefKey, (int)value);
                OnPropertyChanged();
            }
        }

        private double _currentResistance;
        private double _currentCadence;
        private double _currentPower;

        public double CurrentResistance
        {
            get => _currentResistance;
            set { _currentResistance = value; OnPropertyChanged(); }
        }

        /// <summary>Cadence en foulées par minute (elliptique).</summary>
        public double CurrentCadence
        {
            get => _currentCadence;
            set { _currentCadence = value; OnPropertyChanged(); }
        }

        /// <summary>Puissance instantanée en watts (elliptique).</summary>
        public double CurrentPower
        {
            get => _currentPower;
            set { _currentPower = value; OnPropertyChanged(); }
        }

        // Plage de résistance de la machine (lue à la connexion si disponible)
        public double MinResistance { get; private set; } = 1;
        public double MaxResistance { get; private set; } = 25;

        public ObservableCollection<IDevice> DiscoveredDevices { get; set; } = new();

        public event EventHandler<string>? IntervalStatusChanged;
        public event EventHandler<string>? DeviceStatusChanged;

        // Events to notify your ViewModel/UI
        public event EventHandler<double> SpeedUpdated;
        public event EventHandler TreadmillStarted;
        public event EventHandler TreadmillStopped;

        private bool _hasStartedReported = false;

        public string? LastConnectionError { get; private set; }

        private void OnMachineDataUpdated(object? sender, CharacteristicUpdatedEventArgs e)
            => ParseMachineData(e.Characteristic.Value);


        public TreadmillService()
        {
            _ble = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;

            _adapter.DeviceDisconnected += (s, e) => HandleDisconnection(e.Device);
            _adapter.DeviceConnectionLost += (s, e) => HandleDisconnection(e.Device);
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
            await MainThread.InvokeOnMainThreadAsync(DiscoveredDevices.Clear);

            _adapter.ScanMode = ScanMode.LowLatency;   // découverte plus rapide que le mode par défaut
            _adapter.ScanTimeout = 10000; // 10 secondes de scan maximum

            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            _adapter.DeviceAdvertised -= OnDeviceDiscovered;   // capte les noms qui arrivent après le 1er paquet
            _adapter.DeviceAdvertised += OnDeviceDiscovered;

            // Définition des options de scan avec le filtre FTMS
            var scanOptions = new ScanFilterOptions
            {
                ServiceUuids = [FtmsServiceUuid]
            };

            // Durée maximale du scan (ex: 10 secondes)
            //_adapter.ScanTimeout = 10000;

            //Debug.WriteLine("Début du scan des tapis FTMS...");




            Debug.WriteLine("Début du scan BLE...");

            try
            {
                // Pas de filtre d'UUID : beaucoup de tapis n'annoncent pas FTMS dans leur publicité
                //await _adapter.StartScanningForDevicesAsync();
                // Lancement du scan avec le filtre d'UUID
                await _adapter.StartScanningForDevicesAsync(scanOptions);
            }
            finally
            {
                _adapter.DeviceDiscovered -= OnDeviceDiscovered;
                _adapter.DeviceAdvertised -= OnDeviceDiscovered;
            }
        }

        private void OnDeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            var device = e.Device;
            if (device == null) return;

            //string name = e.Device.Name;
            //Collection<AdvertisementRecord> HUID = e.Device.AdvertisementRecords;
            Debug.WriteLine($"Appareil découvert : {device.Name} ({device.Id})");
            // Filtre par mots-clés typiques des tapis de course et équipements de fitness
            /*bool isTreadmill = name.Contains("Treadmill", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Fitness", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Run", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Domyos", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Nordictrack", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("SW7745EAI", StringComparison.OrdinalIgnoreCase);
            */
            /*if ( !DiscoveredDevices.Any(d => d.Id == e.Device.Id)) //isTreadmill &&
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DiscoveredDevices.Add(e.Device);
                });
            }
            */

            // Sans nom pour l'instant : sera ajouté si un paquet suivant en contient un
            if (string.IsNullOrWhiteSpace(device.Name)) return;

            // Test + ajout sur le thread UI pour éviter les doublons et les accès concurrents
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!DiscoveredDevices.Any(d => d.Id == device.Id))
                    DiscoveredDevices.Add(device);
            });
        }

        public async Task StopScanningAsync()
        {
            await _adapter.StopScanningForDevicesAsync();
            Debug.WriteLine("Scan arrêté.");
        }

        public async Task<bool> ConnectToDeviceAsync(IDevice device, CancellationToken cancellationToken = default)
        {
            const int maxAttempts = 3;
            LastConnectionError = null;

            try
            {
                try { await StopScanningAsync(); } catch { /* le scan peut déjà être arrêté */ }
                await Task.Delay(500, cancellationToken);

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        DeviceStatusChanged?.Invoke(this,
                            $"{AppResources.DeviceStatus_Connecting} {device.Name}... ({attempt}/{maxAttempts})");

                        // Annulé soit par l'utilisateur, soit après 15 s
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

                        await _adapter.ConnectToDeviceAsync(
                            device,
                            new ConnectParameters(autoConnect: false, forceBleTransport: true),
                            timeoutCts.Token);

                        var service = await device.GetServiceAsync(FtmsServiceUuid);
                        if (service == null)
                        {
                            LastConnectionError = "Service FTMS (0x1826) introuvable : ce tapis utilise peut-être un protocole propriétaire.";
                            await SafeDisconnectAsync(device);
                            break;
                        }

                        var control = await service.GetCharacteristicAsync(Guid.Parse("00002ad9-0000-1000-8000-00805f9b34fb"));
                        if (control == null)
                        {
                            LastConnectionError = "Caractéristique de contrôle FTMS introuvable.";
                            await SafeDisconnectAsync(device);
                            break;
                        }

                        // Détection automatique du type d'appareil d'après la caractéristique de données exposée
                        var data = await service.GetCharacteristicAsync(TreadmillDataCharacteristicUuid);
                        if (data != null)
                        {
                            MachineType = MachineType.Treadmill;
                        }
                        else
                        {
                            data = await service.GetCharacteristicAsync(CrossTrainerDataCharacteristicUuid);
                            if (data != null)
                                MachineType = MachineType.CrossTrainer;
                            // sinon : on garde le type choisi par l'utilisateur
                        }

                        if (_dataCharacteristic != null)
                            _dataCharacteristic.ValueUpdated -= OnMachineDataUpdated;

                        _controlPointCharacteristic = control;
                        _dataCharacteristic = data;

                        if (data != null)
                        {
                            data.ValueUpdated += OnMachineDataUpdated;
                            await data.StartUpdatesAsync();
                        }

                        if (MachineType == MachineType.CrossTrainer)
                            await ReadResistanceRangeAsync(service);

                        _treadmillDevice = device;
                        IsConnected = true;
                        DeviceStatusChanged?.Invoke(this, AppResources.DeviceStatus_Connected);
                        return true;
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested)
                    {
                        // Annulation par l'utilisateur (quelle que soit l'exception levée par le plugin)
                        throw new OperationCanceledException(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LastConnectionError = ex.Message;
                        Debug.WriteLine($"Connexion, tentative {attempt}/{maxAttempts} : {ex}");

                        await SafeDisconnectAsync(device);
                        if (attempt < maxAttempts)
                            await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Connexion annulée par l'utilisateur.");
                await SafeDisconnectAsync(device);   // stoppe la tentative native encore en cours

                LastConnectionError = null;
                IsConnected = false;
                DeviceStatusChanged?.Invoke(this, AppResources.DeviceStatus_Disconnected);
                return false;
            }

            IsConnected = false;
            DeviceStatusChanged?.Invoke(this, $"{AppResources.Error} : {LastConnectionError}");
            return false;
        }

        private async Task SafeDisconnectAsync(IDevice device)
        {
            try { await _adapter.DisconnectDeviceAsync(device); }
            catch (Exception ex) { Debug.WriteLine($"Déconnexion : {ex.Message}"); }
        }

        public async Task StartAsync()
        {
            IsRunning = true;
            SetKeepScreenOn(true);
            if (_controlPointCharacteristic != null)
            {
                await _controlPointCharacteristic.WriteAsync(new byte[] { 0x00 });
                // Start or Resume : sans paramètre en FTMS (le tapis historique tolère l'octet 0x01)
                byte[] start = MachineType == MachineType.CrossTrainer
                    ? new byte[] { 0x07 }
                    : new byte[] { 0x07, 0x01 };
                await _controlPointCharacteristic.WriteAsync(start);
            }
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            IsRunning = false;
            SetKeepScreenOn(false);
            _workoutCancellationToken?.Cancel();
            CurrentSpeed = 0;
            CurrentCadence = 0;
            CurrentPower = 0;

            if (_controlPointCharacteristic != null)
            {
                await _controlPointCharacteristic.WriteAsync(new byte[] { 0x08, 0x01 });
            }
            await Task.CompletedTask;
        }

        private static void SetKeepScreenOn(bool keepOn)
        {
            if (MainThread.IsMainThread)
                Apply(keepOn);
            else
                MainThread.BeginInvokeOnMainThread(() => Apply(keepOn));

            static void Apply(bool on)
            {
                try
                {
                    if (DeviceDisplay.Current.KeepScreenOn != on)
                        DeviceDisplay.Current.KeepScreenOn = on;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"KeepScreenOn failed: {ex.Message}");
                }
            }
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

        public async Task SetResistanceAsync(double level)
        {
            CurrentResistance = Math.Max(MinResistance, Math.Min(MaxResistance, level));

            if (_controlPointCharacteristic != null && IsRunning && MachineType == MachineType.CrossTrainer)
            {
                // Set Target Resistance Level (0x04) : uint8, résolution 0,1
                byte raw = (byte)Math.Round(Math.Clamp(CurrentResistance * 10.0, 0, 255));
                await _controlPointCharacteristic.WriteAsync(new byte[] { 0x04, raw });
            }
        }

        /// <summary>Lit la plage de résistance supportée (0x2AD6). Valeurs par défaut conservées si absente.</summary>
        private async Task ReadResistanceRangeAsync(IService service)
        {
            MinResistance = 1;
            MaxResistance = 25;

            try
            {
                var range = await service.GetCharacteristicAsync(ResistanceRangeCharacteristicUuid);
                if (range == null) return;

                var (bytes, _) = await range.ReadAsync();
                if (bytes is { Length: >= 2 })
                {
                    // Min / Max : sint8, résolution 0,1
                    double min = (sbyte)bytes[0] / 10.0;
                    double max = (sbyte)bytes[1] / 10.0;
                    if (max > min)
                    {
                        MinResistance = min;
                        MaxResistance = max;
                    }
                }
                Debug.WriteLine($"Plage de résistance : {MinResistance} - {MaxResistance}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lecture de la plage de résistance impossible : {ex.Message}");
            }
        }

        private void ParseMachineData(byte[] data)
        {
            if (MachineType == MachineType.CrossTrainer)
                ParseCrossTrainerData(data);
            else
                ParseTreadmillData(data);
        }

        /// <summary>Cross Trainer Data (0x2ACE) : flags sur 3 octets, puis champs dans l'ordre de la spec FTMS.</summary>
        private void ParseCrossTrainerData(byte[] data)
        {
            if (data == null || data.Length < 5) return;

            int flags = data[0] | (data[1] << 8) | (data[2] << 16);
            int pos = 3;

            bool Has(int bit) => (flags & (1 << bit)) != 0;
            bool Can(int n) => pos + n <= data.Length;
            int U16(int p) => data[p] | (data[p + 1] << 8);
            short S16(int p) => (short)(data[p] | (data[p + 1] << 8));

            double speed = CurrentSpeed;
            double cadence = CurrentCadence;
            double power = CurrentPower;

            // Bit 0 = « More Data » : la vitesse instantanée est présente quand il vaut 0
            if (!Has(0) && Can(2)) { speed = U16(pos) / 100.0; pos += 2; }   // 0,01 km/h
            if (Has(1)) pos += 2;                                              // vitesse moyenne
            if (Has(2)) pos += 3;                                              // distance totale

            if (Has(3))                                                        // foulées/min + cadence moyenne
            {
                if (Can(2)) { int spm = U16(pos); if (spm != 0xFFFF) cadence = spm; }
                pos += 4;
            }

            if (Has(4)) pos += 2;                                              // nombre de foulées
            if (Has(5)) pos += 4;                                              // dénivelé + / -
            if (Has(6)) pos += 4;                                              // inclinaison + angle de rampe

            if (Has(7))                                                        // niveau de résistance
            {
                if (Can(2))
                {
                    short r = S16(pos);
                    // Ignore une valeur hors plage (certaines machines utilisent une autre échelle)
                    if (r >= MinResistance && r <= MaxResistance)
                        CurrentResistance = r;
                }
                pos += 2;
            }

            if (Has(8))                                                        // puissance instantanée (W)
            {
                if (Can(2)) { short p = S16(pos); if (p != short.MaxValue) power = p; }
                pos += 2;
            }

            CurrentSpeed = speed;
            CurrentCadence = cadence;
            CurrentPower = power;
            SpeedUpdated?.Invoke(this, speed);

            // Sur un elliptique, « en mouvement » = cadence ou vitesse non nulle
            bool moving = cadence > 0 || speed > 0.5;
            bool stopped = cadence <= 0 && speed <= 0.1;

            if (moving && !_hasStartedReported)
            {
                _hasStartedReported = true;
                TreadmillStarted?.Invoke(this, EventArgs.Empty);
                Debug.WriteLine("Cross trainer movement detected: STARTED");
            }
            else if (stopped && _hasStartedReported)
            {
                _hasStartedReported = false;
                TreadmillStopped?.Invoke(this, EventArgs.Empty);
                Debug.WriteLine("Cross trainer movement detected: STOPPED");
            }
        }

        private void ParseTreadmillData(byte[] data)
        {
            if (data == null || data.Length < 4) return;

            // Parse Instantaneous Speed (Standard FTMS specification)
            // Bytes 0-1: Flags, Bytes 2-3: Instantaneous Speed (in 0.01 km/h)
            int flags = data[0] | (data[1] << 8);
            int pos = 2;

            bool Has(int bit) => (flags & (1 << bit)) != 0;

            // Bit 0 = « More Data » : la vitesse instantanée est présente quand il vaut 0
            if (!Has(0) && pos + 2 <= data.Length)
            {
                CurrentSpeed = (data[pos] | (data[pos + 1] << 8)) / 100.0;   // 0,01 km/h
                pos += 2;

                SpeedUpdated?.Invoke(this, CurrentSpeed);

                if (CurrentSpeed > 0.5 && !_hasStartedReported)
                {
                    _hasStartedReported = true;
                    TreadmillStarted?.Invoke(this, EventArgs.Empty);
                    Debug.WriteLine("Treadmill movement detected: STARTED");
                }
                else if (CurrentSpeed <= 0.1 && _hasStartedReported)
                {
                    _hasStartedReported = false;
                    TreadmillStopped?.Invoke(this, EventArgs.Empty);
                    Debug.WriteLine("Treadmill movement detected: STOPPED");
                }
            }

            if (Has(1)) pos += 2;   // vitesse moyenne
            if (Has(2)) pos += 3;   // distance totale

            // Bit 3 : inclinaison (sint16, 0,1 %) + angle de rampe (sint16)
            if (Has(3) && pos + 2 <= data.Length)
            {
                short raw = (short)(data[pos] | (data[pos + 1] << 8));
                if (raw != short.MaxValue)   // 0x7FFF = valeur non disponible
                    CurrentIncline = raw / 10.0;
            }
        }

        public async Task RunWorkoutPlanAsync(WorkoutPlan plan, Action<string, int> onProgress)
        {
            if (_isWorkoutRunning) return;   // évite deux entraînements en parallèle
            _isWorkoutRunning = true;

            _workoutCancellationToken?.Dispose();
            _workoutCancellationToken = new CancellationTokenSource();
            var token = _workoutCancellationToken.Token;

            // Copie des intervalles : modifier la liste pendant la séance n'a aucun effet sur elle
            var intervals = plan.Intervals.ToList();

            IsPaused = false;
            string finalStatus = AppResources.Lbl_Status_Run;   // statut affiché si la séance est annulée

            try
            {
                await StartAsync();
                IntervalStatusChanged?.Invoke(this, $"{AppResources.IntervalStatus_Starting_Workout} [{plan.Title}]");

                // Attend que la bande se mette réellement en mouvement (Stop annule l'attente)
                await WaitForMovementAsync(token);

                await PlayBeepSoundAsync();   // vérifie lui-même le réglage des alertes sonores

                for (int i = 0; i < intervals.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var interval = intervals[i];
                    bool isLast = i == intervals.Count - 1;

                    if (AppSettingsService.Instance.EnableCountdown)
                        await RunCountdownAsync(interval, token);

                    await ApplyIntervalTargetsAsync(interval);
                    // (cibles appliquées ci-dessus selon le type d'appareil)
                    IntervalStatusChanged?.Invoke(this, $"{AppResources.IntervalStatus_Starting} : {interval.Name}");

                    await RunEffortAsync(interval, onProgress, token);

                    // Pas de récupération après le dernier intervalle
                    if (!isLast && interval.EnableRest && interval.RestSecondsAfter > 0)
                        await RunRestAsync(interval, token);
                }

                finalStatus = AppResources.IntervalStatus_Completed;
            }
            catch (OperationCanceledException)
            {
                // Arrêt demandé par l'utilisateur : rien à faire
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur pendant l'entraînement : {ex}");
                finalStatus = $"{AppResources.Error} : {ex.Message}";
            }
            finally
            {
                IsPaused = false;

                try
                {
                    // Si c'est StopAsync qui a annulé, il a déjà envoyé la commande d'arrêt
                    if (!token.IsCancellationRequested)
                        await StopAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erreur à l'arrêt : {ex.Message}");
                }

                IntervalStatusChanged?.Invoke(this, finalStatus);
                _isWorkoutRunning = false;
            }
        }

        private async Task WaitForMovementAsync(CancellationToken token)
        {
            if (MachineType == MachineType.Treadmill)
            {
                await WaitForTreadmillToMoveAsync(token);
                return;
            }

            // Elliptique : on attend que l'utilisateur pédale (60 s max, car certaines
            // machines ne remontent ni vitesse ni cadence)
            IntervalStatusChanged?.Invoke(this, AppResources.IntervalStatus_Waiting_Movement);
            for (int i = 0; i < 120 && !_hasStartedReported; i++)
                await Task.Delay(500, token);
        }

        private async Task ApplyIntervalTargetsAsync(WorkoutInterval interval)
        {
            if (MachineType == MachineType.CrossTrainer)
            {
                await SetResistanceAsync(interval.TargetResistance);
            }
            else
            {
                await SetSpeedAsync(interval.TargetSpeed);
                await SetInclineAsync(interval.TargetIncline);
            }
        }

        private async Task ApplyRestTargetsAsync()
        {
            if (MachineType == MachineType.CrossTrainer)
            {
                await SetResistanceAsync(RestResistanceLevel);
            }
            else
            {
                await SetSpeedAsync(RestSpeedKmH);
                await SetInclineAsync(0);
            }
        }

        private async Task WaitForTreadmillToMoveAsync(CancellationToken token)
        {
            while (!_hasStartedReported)
                await Task.Delay(500, token);
        }

        private async Task WaitWhilePausedAsync(CancellationToken token)
        {
            while (IsPaused)
                await Task.Delay(300, token);
        }

        private async Task RunCountdownAsync(WorkoutInterval interval, CancellationToken token)
        {
            for (int c = CountdownSeconds; c > 0; c--)
            {
                await WaitWhilePausedAsync(token);
                IntervalStatusChanged?.Invoke(this, $"{AppResources.Txt_Ready_For} [{interval.Name}] {AppResources.Txt_In} {c}s...");
                await Task.Delay(1000, token);
            }
        }

        private async Task RunEffortAsync(WorkoutInterval interval, Action<string, int> onProgress, CancellationToken token)
        {
            int total = (int)Math.Round(interval.DurationSeconds);

            for (int remaining = total; remaining > 0; remaining--)
            {
                await WaitWhilePausedAsync(token);

                // Bip 5 secondes avant la fin de l'intervalle (sans bloquer le chrono)
                if (remaining == 5)
                    _ = PlayBeepSoundAsync();

                onProgress(interval.Name, remaining);
                await Task.Delay(1000, token);
            }
        }

        private async Task RunRestAsync(WorkoutInterval interval, CancellationToken token)
        {
            await ApplyRestTargetsAsync();
            // (récupération appliquée ci-dessus selon le type d'appareil)

            for (int r = interval.RestSecondsAfter; r > 0; r--)
            {
                await WaitWhilePausedAsync(token);
                IntervalStatusChanged?.Invoke(this, $"{AppResources.Txt_Rest_Pause} : {r}s");
                await Task.Delay(1000, token);
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

        public string PaceFormattedString(string speedString)
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

        private void HandleDisconnection(IDevice? device)
        {
            // Ignore les autres appareils BLE
            if (device == null || _treadmillDevice == null || device.Id != _treadmillDevice.Id)
                return;

            Debug.WriteLine($"Tapis déconnecté : {device.Name}");

            // Arrête la séance en cours (le finally de RunWorkoutPlanAsync nettoie le reste)
            _workoutCancellationToken?.Cancel();

            if (_dataCharacteristic != null)
                _dataCharacteristic.ValueUpdated -= OnMachineDataUpdated;

            _dataCharacteristic = null;
            _controlPointCharacteristic = null;
            _treadmillDevice = null;
            _hasStartedReported = false;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsRunning = false;
                IsConnected = false;
                CurrentSpeed = 0;
                CurrentIncline = 0;
                CurrentResistance = 0;
                CurrentCadence = 0;
                CurrentPower = 0;
                SetKeepScreenOn(false);
                DeviceStatusChanged?.Invoke(this, AppResources.DeviceStatus_Disconnected);
            });
        }

    }
}