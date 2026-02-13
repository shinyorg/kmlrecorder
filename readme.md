# Shiny KML Recorder


- Platforms/iOS/Info.plist — Added UIApplicationSceneManifest with a CPTemplateApplicationSceneSessionRoleApplication
  scene configuration pointing to CarPlaySceneDelegate
- ShinyKmlRecorder.csproj — Added CustomEntitlements for com.apple.developer.carplay-driving-task

Note: You'll also need to enable the CarPlay entitlement in your Apple Developer portal for your App ID (
org.shiny.kmlrecord) — specifically the "Driving Task" CarPlay capability.


<PropertyGroup>
    <AddCarplay></AddCarplay>
</PropertyGroup>