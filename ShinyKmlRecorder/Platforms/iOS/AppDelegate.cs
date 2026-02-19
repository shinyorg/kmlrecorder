using Foundation;
using UIKit;

namespace ShinyKmlRecorder;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp()
        => MauiProgram.CreateMauiApp();
    
#if ADD_CAR_APPS
    [Export("application:configurationForConnectingSceneSession:options:")]
    public override UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
    {
        if (connectingSceneSession.Role.GetConstant() == UIWindowSceneSessionRole.CarTemplateApplication.GetConstant())
        {
            var config = new UISceneConfiguration("CarPlay", connectingSceneSession.Role);
            config.DelegateType = typeof(CarPlaySceneDelegate);
            return config;
        }
        var defaultConfig = base.GetConfiguration(application, connectingSceneSession, options);
        defaultConfig.DelegateType = typeof(SceneDelegate);
        return defaultConfig;
    }
#endif
}