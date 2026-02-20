# Shiny KML Recorder

A .NET MAUI mobile application for iOS and Android that records GPS coordinates and exports them as **KML** or **GeoJSON** files. Track your trips with check-in/check-out sessions, monitor battery and GPS data, and deliver exports via email, Azure Blob Storage, system share, or local storage. Also supports **Apple CarPlay** and **Android Auto** for hands-free recording control.

## Features

### GPS Tracking
- Real-time GPS listening with high-accuracy requests via Shiny.Locations
- Records latitude, longitude, accuracy, speed (m/s & km/h), and timestamp for each reading
- Android foreground service notification keeps recording active in the background

### Trip Management
- **Check-in** starts a new trip session (generates a unique WorkId)
- **Check-out** ends the current session
- All GPS pings are grouped by trip
- Export filtering by individual trip or all trips at once
- Trip labels display start time, end time, and point count

### Export Formats
- **KML 2.2** — Document structure with styled point placemarks (circle icons, labels) and a track LineString showing the route. Point descriptions include accuracy, speed, battery level, and timestamp.
- **GeoJSON** — FeatureCollection with Point features and a LineString track. Properties include timestamp, accuracy, speed, battery level, battery status, and energy saver state.

### Export Delivery Methods
- **Local** — Save the exported file to the device
- **Email** — Attach the file to an email using the platform email service
- **Share** — Open the system share dialog
- **Azure Blob Storage** — Upload via PUT request with a SAS token (auto-detects content type)

### Battery & System Monitoring
- Tracks battery level, charge state (Charging / Discharging / Not Charging / Full / Unknown), and energy saver status alongside every GPS reading
- GPS permission status displayed on the main screen

### Settings Persistence
- Selected trip, export format, delivery method, Azure credentials, and email settings are saved to app preferences and persist across restarts

### CarPlay & Android Auto
- **Apple CarPlay** — Grid template with a Start/Stop recording button; updates live as recording state changes
- **Android Auto** — Pane template with a Start/Stop action button via AndroidX Car App library

## Pages

| Page | Description |
|---|---|
| **Main** | GPS recording control — Check-in / Check-out toggle, session status, GPS permission state, navigation to Logs & Settings |
| **Logs** | Displays all recorded GPS points with timestamps, coordinates, accuracy, battery level, and energy saver status |
| **Export** | Shows GPS point count, export status summary, Export Now button, and Open Export Folder |
| **Export Settings** | Configure trip selection, export format (KML / GeoJSON), delivery method, Azure Blob credentials, and email settings; includes a Reset option |

## Third-Party Libraries

| Package | Version | Description | Documentation |
|---|---|---|---|
| [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) | 8.4.0 | MVVM source generators and helpers (`[ObservableProperty]`, `[RelayCommand]`) | [Docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) |
| [Microsoft.Maui.Controls](https://www.nuget.org/packages/Microsoft.Maui.Controls) | 10.0.30 | .NET MAUI UI framework | [Docs](https://learn.microsoft.com/en-us/dotnet/maui/) |
| [Microsoft.Extensions.Logging.Debug](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Debug) | 10.0.0 | Debug output logging provider | [Docs](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) |
| [Sentry.Maui](https://www.nuget.org/packages/Sentry.Maui) | 6.1.0 | Error tracking and performance monitoring | [Docs](https://docs.sentry.io/platforms/dotnet/guides/maui/) |
| [Shiny.Hosting.Maui](https://www.nuget.org/packages/Shiny.Hosting.Maui) | 4.0.0-beta-0095 | Shiny platform hosting for MAUI | [Docs](https://shinylib.net/) |
| [Shiny.Locations](https://www.nuget.org/packages/Shiny.Locations) | 4.0.0-beta-0095 | GPS manager, readings, and location requests | [Docs](https://shinylib.net/client/locations/gps/) |
| [Shiny.Maui.Shell](https://www.nuget.org/packages/Shiny.Maui.Shell) | 2.0.0 | Shell navigation and source-generated route mapping | [Docs](https://github.com/shinyorg/mauishell) |
| [Shiny.Extensions.DependencyInjection](https://www.nuget.org/packages/Shiny.Extensions.DependencyInjection) | 1.3.1 | Attribute-driven DI registration with source generators | [Docs](https://github.com/shinyorg/extensions) |
| [Shiny.Reflector](https://www.nuget.org/packages/Shiny.Reflector) | 1.7.1 | AOT-compliant source-generated reflection utilities | [Docs](https://github.com/shinyorg/reflector) |
| [sqlite-net-pcl](https://www.nuget.org/packages/sqlite-net-pcl) | 1.10.196-beta | Lightweight SQLite ORM for .NET | [Docs](https://github.com/praeclarum/sqlite-net) |
| [SQLitePCLRaw.bundle_e_sqlite3](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3) | 3.0.2 | Native SQLite provider (Android) | [Docs](https://github.com/ericsink/SQLitePCL.raw) |
| [Xamarin.AndroidX.Car.App.App](https://www.nuget.org/packages/Xamarin.AndroidX.Car.App.App) | 1.7.0.2 | Android Auto Car App UI library | [Docs](https://developer.android.com/training/cars) |
| [Xamarin.AndroidX.Lifecycle.LiveData.Core](https://www.nuget.org/packages/Xamarin.AndroidX.Lifecycle.LiveData.Core) | 2.10.0.1 | AndroidX lifecycle-aware LiveData components | [Docs](https://developer.android.com/topic/libraries/architecture/livedata) |

## Platform Requirements

- **iOS**: iOS 16.0+
- **Android**: API 26+ (Android 8.0)
- **App ID**: `org.shiny.kmlrecord`

### CarPlay Setup

1. Enable the **Driving Task** CarPlay capability for your App ID (`org.shiny.kmlrecord`) in the [Apple Developer Portal](https://developer.apple.com/account/resources/identifiers/list)
2. The project includes `CarPlaySceneDelegate` and the required `Info.plist` scene manifest configuration
3. Custom entitlements for `com.apple.developer.carplay-driving-task` are set in the `.csproj`

To toggle CarPlay/Android Auto support, set the `AddCarplay` property in the `.csproj`:

```xml
<PropertyGroup>
    <AddCarplay>true</AddCarplay>
</PropertyGroup>
```