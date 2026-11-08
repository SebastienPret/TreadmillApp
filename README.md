# 🏃‍♂️ TreadmillApp – .NET MAUI Treadmill Controller

A mobile and desktop app built with **.NET MAUI** to control connected treadmills or cross trainer that support the standard Bluetooth **FTMS (Fitness Machine Service)** protocol, and to create and manage custom interval training programs.

**Platforms:** Android · Windows · iOS (planned)
**Languages:** English · Français (Belgium) · Nederlands (Belgium)

---

## 🚀 Main Features

1. **Dashboard**
   * Live control of **speed** and **incline**.
   * **START** and **STOP** commands.
   * Real-time display of speed, pace (min/km) and incline reported by the treadmill.
   * Live workout status (current interval, countdown, remaining time).
   * Connection button that reflects the connection state.

2. **Bluetooth LE (FTMS) Connection**
   * Scan for nearby Bluetooth devices.
   * Permission and Bluetooth-state checks before every scan (Android / iOS).
   * Connection with retries and a **Cancel** button; other treadmills cannot be selected while a connection is in progress.
   * Control point (`0x2AD9`) and treadmill data (`0x2ACD`) characteristics.

3. **Interval Creator and Editor**
   * Custom effort blocks (name, target speed, incline, duration).
   * Automatic **km/h → min/km** pace conversion.
   * Optional **automatic recovery pause** after each interval.
   * Add, **edit in place** and delete intervals. Editing an interval does not affect the ones after it.

4. **Workout Management**
   * Plans are saved on the device (`Preferences` / JSON).
   * Dedicated page to list, run, edit or delete saved workouts.
   * **Start workout** from the interval creator launches the plan directly on the Dashboard.

5. **Workout Experience**
   * Optional 5-second **countdown** before each interval.
   * Optional **audio alerts** (beep before the end of an interval and at the end of the workout).
   * The **screen stays on** during a workout.

6. **Settings**
   * App language (English / French / Dutch).
   * **Themes**: Dark, Light and Ocean, switchable at runtime and remembered between launches.
   * Workout options (countdown, audio alerts).
   * Manual **update check**.
   * Reset saved data.

7. **In-app Updates (outside the stores)**
   * Checks once a day at startup, and manually from Settings.
   * Android: downloads the APK and opens the system installer.
   * Windows: downloads the installer and runs it silently, then relaunches the app.
   * iOS: opens the TestFlight / App Store link (Apple does not allow direct installation).

---

## 🛠️ Tech Stack

* **Framework:** .NET MAUI (.NET 10)
* **Language:** C#
* **Bluetooth:** [`Plugin.BLE`](https://github.com/dotnet-bluetooth-le/dotnet-bluetooth-le)
* **MVVM:** [`CommunityToolkit.Mvvm`](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
* **Audio:** [`Plugin.Maui.Audio`](https://github.com/jfversluis/Plugin.Maui.Audio)
* **Architecture:** MVVM with services and a few code-behind handlers
* **Windows installer:** [Inno Setup](https://jrsoftware.org/isinfo.php)

---

## 📦 Installation & Requirements

### 1. Requirements

* [.NET 10 SDK](https://dotnet.microsoft.com/)
* **Visual Studio** with the *.NET Multi-platform App UI development* workload (a version that supports .NET 10), or **VS Code** with the .NET MAUI extension.
* For iOS: a Mac (or a Windows PC paired to a Mac) with Xcode, and an Apple Developer account.

### 2. NuGet packages

Restored automatically from the `.csproj`. To add them manually:

```bash
dotnet add package Plugin.BLE
dotnet add package CommunityToolkit.Mvvm
dotnet add package Plugin.Maui.Audio
```

### 3. Platform configuration

**Android** – `Platforms/Android/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.BLUETOOTH" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" android:usesPermissionFlags="neverForLocation" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.REQUEST_INSTALL_PACKAGES" />
```

The in-app updater also needs a `FileProvider` (see [Updates](#-updates)).

**iOS** – `Platforms/iOS/Info.plist`:

```xml
<key>NSBluetoothAlwaysUsageDescription</key>
<string>Required to connect to your treadmill.</string>
<key>ITSAppUsesNonExemptEncryption</key>
<false/>
```

**Windows** – the project is built *unpackaged* (`WindowsPackageType=None`). If you switch to MSIX, declare the Bluetooth capability in `Package.appxmanifest`:

```xml
<Capabilities>
  <DeviceCapability Name="bluetooth" />
</Capabilities>
```

---

## 📁 Project Structure

```text
TreadmillApp/
│
├── Models/
│   ├── MachineType.cs             # Machine type models
│   └── WorkoutInterval.cs         # Interval and workout plan models
│
├── ViewModels/
│   ├── DashboardViewModel.cs
│   ├── IntervalsViewModel.cs
│   ├── SavedWorkoutsViewModel.cs
│   ├── ScanDevicesViewModel.cs
│   └── SettingsViewModel.cs
│
├── Views/
│   ├── DashboardPage.xaml         # Main dashboard and live control
│   ├── ScanDevicesPage.xaml       # Bluetooth search and connection
│   ├── IntervalsPage.xaml         # Interval creator / editor
│   ├── SavedWorkoutsPage.xaml     # Saved workouts list
│   └── SettingsPage.xaml          # Language, theme, options, updates
│
├── Services/
│   ├── TreadmillService.cs        # BLE (FTMS) communication and workout engine
│   ├── BlePermissionHelper.cs     # Bluetooth permissions / adapter state
│   ├── WorkoutStorageService.cs   # Saving and loading workouts
│   ├── AppStateService.cs         # Shared state (current plan, auto-start flag)
│   ├── AppSettingsService.cs      # User settings (language, options)
│   ├── LocalizationService.cs     # Runtime language switching
│   ├── ThemeService.cs            # Runtime theme switching
│   └── UpdateService.cs           # Update check, download and install
│
├── Resources/
│   ├── Strings/
│   │   ├── AppResources.resx          # Default (English)
│   │   ├── AppResources.fr-BE.resx    # French (Belgium)
│   │   └── AppResources.nl-BE.resx    # Dutch (Belgium)
│   ├── Styles/
│   │   ├── Colors.xaml            # Fixed brand palette
│   │   └── Styles.xaml            # Global styles (DynamicResource)
│   ├── Themes/
│   │   ├── DarkTheme.xaml
│   │   ├── LightTheme.xaml
│   │   ├── OceanTheme.xaml
│   │   └── Themes.cs              # Code-behind of the theme dictionaries
│   ├── AppIcon/                   # appicon.svg + appiconfg.svg
│   └── Splash/
│
├── Platforms/                     # Android, iOS, Windows, MacCatalyst specifics
├── installer/
│   └── TreadmillApp.iss           # Inno Setup script (Windows installer)
│
├── AppShell.xaml                  # Navigation (flyout menu)
└── App.xaml.cs                    # Entry point, theme init, startup update check
```

---

## 📱 Usage

1. Launch the app on your device (Android, Windows or iOS).
2. On the **Dashboard**, tap the connection button to search for a treadmill.
3. Allow the Bluetooth permissions when asked, then select your treadmill in the list. Tap **Cancel** if the connection takes too long.
4. In **Intervals**, build a workout by adding intervals (with or without automatic recovery pauses), or pick one from **Saved workouts**.
5. Tap **Start workout** to launch it directly, or run it later from the Dashboard.
6. Change the language or the theme any time in **Settings**.

---

## 🎨 Themes

Colors are split into three layers:

* `Colors.xaml` – fixed brand colors.
* `Themes/*.xaml` – the **same semantic keys** in every theme (`PageBackground`, `SurfaceColor`, `TextPrimary`, `AccentColor`, `HighlightColor`, …) with different values.
* `Styles.xaml` – styles that use `DynamicResource`, so they update instantly when the theme changes.

**Add a new theme:**

1. Copy `OceanTheme.xaml`, rename it, and change the colors and the `x:Class`.
2. Add a matching class in `Themes.cs`.
3. Add a value to the `AppStyle` enum and a case in `ThemeService.Apply`.

In pages, always use styles (`Style="{StaticResource PrimaryButton}"`) and `DynamicResource` for colors. Hard-coded colors do not follow the theme.

---

## 🔄 Updates

The app reads a small `version.json` file hosted online (for example in the assets of a GitHub Release):

```json
{
  "version": "1.1.0",
  "notes": {
    "en": "New theme, BLE fixes.",
    "fr": "Nouveau thème, corrections BLE.",
    "nl": "Nieuw thema, BLE-fixes."
  },
  "android": { "url": "https://github.com/OWNER/REPO/releases/download/v1.1.0/TreadmillApp.apk", "sha256": "…" },
  "windows": { "url": "https://github.com/OWNER/REPO/releases/download/v1.1.0/TreadmillApp-Setup.exe", "sha256": "…" },
  "ios":     { "url": "https://apps.apple.com/app/idXXXXXXXXX" }
}
```

* The check runs at most **once every 24 hours** at startup, and any time from **Settings → Check for updates**.
* If the network is unavailable, the check fails silently and is retried at the next launch.
* Set the manifest URL in `UpdateService.cs` (`ManifestUrl`).
* **Android** needs a `FileProvider` with the authority `<ApplicationId>.fileprovider`, a `file_paths.xml` resource (`<cache-path name="updates" path="updates/" />`), and the user must allow *Install unknown apps* for the app.
* **If you ever publish on Google Play**, remove the self-update code from that build (Play policy forbids updating outside the store).

**Release checklist**

1. Increase `ApplicationDisplayVersion` (e.g. `1.1.0`) **and** `ApplicationVersion` (integer, +1) in the `.csproj`.
2. Build the Android APK with the **same signing key** as before.
3. Build the Windows installer (see below).
4. Compute the SHA-256 of each file and update `version.json`.
5. Create the GitHub Release and upload the APK, the installer and `version.json`.

---

## 📦 Building & Publishing

### Android (APK)

Create a signing key once (and keep it safe – losing it prevents future updates):

```bash
keytool -genkey -v -keystore treadmillapp.keystore -alias treadmillapp -keyalg RSA -keysize 2048 -validity 10000
```

Set the password as an environment variable, then publish:

```powershell
setx AndroidSigningPassword "your-password"   # then reopen Visual Studio / the terminal
dotnet publish -f net10.0-android -c Release
```

The signed `.apk` is in `bin/Release/net10.0-android/publish/`.
For Google Play, set `AndroidPackageFormat` to `aab` instead.

### Windows (unpackaged + installer)

```powershell
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsAppSDKSelfContained=true -p:SelfContained=true

"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DMyAppVersion=1.1.0 installer\TreadmillApp.iss

certutil -hashfile installer\Output\TreadmillApp-Setup.exe SHA256
```

The installer is per-user (no administrator rights needed), available in English, French and Dutch. Windows SmartScreen shows a warning for unsigned installers unless you sign them with a code-signing certificate.

### iOS (on a Mac)

Requires an Apple Developer account, a distribution certificate and a provisioning profile. Configure `CodesignKey` and `CodesignProvision` in the `.csproj`, then:

```bash
dotnet publish -f net10.0-ios -c Release
```

Upload the resulting `.ipa` with **Transporter** (TestFlight / App Store).

> ⚠️ Always test the **Release** build on a real device before distributing it: trimming can break reflection-based bindings and plugins that work fine in Debug.

---

## 🩺 Troubleshooting

| Problem | What to check |
|---|---|
| Treadmill is not listed | Make sure it is not already connected to another phone or app (only one BLE connection is usually allowed). On Android 11 and below, location services must be on. |
| Connection fails | Power-cycle the treadmill and remove any system Bluetooth pairing. The error message shown in the alert tells you the reason. |
| "FTMS service not found" | The treadmill probably uses a proprietary protocol and is not FTMS-compatible. |
| Status bar / title bar color is wrong on Android | Apply the theme colors on the `Shell` and clear `bin`/`obj`, then reinstall the app. |
| Build error `XA4314` (signing) | The `AndroidSigningPassword` environment variable is not visible to Visual Studio. Define it with `setx` and restart Visual Studio. |

---

## ✅ Roadmap / To-do

- [x] App icon
- [x] Languages (EN / FR / NL)
- [x] Colors and runtime themes (Dark / Light / Ocean)
- [x] Search for other treadmills
- [x] Dynamic incline button
- [x] Select an interval to edit (refresh)
- [x] Fix seconds rounding on the interval page
- [x] Remove the 5-second sound gap between intervals
- [x] Add a sound at the end of the workout
- [x] Launch button in the interval creator starts the workout instead of just selecting it
- [x] Edit an interval without deleting the ones after it
- [x] Convert km/h to min/km
- [x] Check Android / iOS permissions before scanning
- [x] Keep the screen on during a workout
- [x] Detect when the treadmill turns off or disconnects, and update the connection state (button, status, speed and incline) accordingly
- [ ] Read incline changes made by the treadmill itself (FTMS data / status characteristics)
- [x] Settings page: refresh the page after a language change
- [ ] Upload a custom sound
- [ ] Connection with MyWoosh
- [ ] Pause / resume button on the Dashboard
- [ ] In-app updates: test end to end on Android and Windows
- [ ] "Skip this version" option in the update prompt
- [ ] Sign the Windows installer (code-signing certificate)
- [ ] iOS release (TestFlight / App Store)
- [ ] Keep the workout timer running with the screen locked (Android foreground service)
- [ ] Support for cross trainer (Testing needed)