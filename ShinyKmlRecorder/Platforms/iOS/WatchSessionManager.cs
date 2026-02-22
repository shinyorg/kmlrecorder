using Foundation;
using Shiny.Locations;
using WatchConnectivity;

namespace ShinyKmlRecorder;

[Register("WatchSessionManager")]
public class WatchSessionManager : WCSessionDelegate
{
    static WatchSessionManager? instance;

    public static void Start()
    {
        if (!WCSession.IsSupported)
            return;

        instance = new WatchSessionManager();
        WCSession.DefaultSession.Delegate = instance;
        WCSession.DefaultSession.ActivateSession();
    }

    public override void SessionReachabilityDidChange(WCSession session)
    {
        if (session.Reachable)
            SendCurrentState(session);
    }

    public override void DidReceiveMessage(WCSession session, NSDictionary<NSString, NSObject> message, WCSessionReplyHandler replyHandler)
    {
        var action = (message[new NSString("action")] as NSString)?.ToString();

        if (action == "toggle")
        {
            _ = HandleToggle(replyHandler);
        }
        else if (action == "getState")
        {
            replyHandler(BuildStateDict());
        }
    }

    async Task HandleToggle(WCSessionReplyHandler replyHandler)
    {
        try
        {
            var services = IPlatformApplication.Current!.Services;
            var logService = services.GetRequiredService<ILogService>();
            var gpsManager = services.GetRequiredService<IGpsManager>();

            if (logService.DateCheckedIn != null)
            {
                if (gpsManager.IsListening())
                    await gpsManager.StopListener();

                await logService.Checkout();
            }
            else
            {
                await logService.Checkin();

                if (!gpsManager.IsListening())
                    await gpsManager.StartListener(GpsRequest.Realtime(true));
            }
        }
        catch { }

        replyHandler(BuildStateDict());
    }

    static NSDictionary<NSString, NSObject> BuildStateDict()
    {
        var logService = IPlatformApplication.Current?.Services?.GetService<ILogService>();
        var isRecording = logService?.DateCheckedIn != null;

        var keys = new List<NSString> { new("isRecording") };
        var values = new List<NSObject> { new NSNumber(isRecording) };

        if (isRecording && logService?.DateCheckedIn is { } checkedIn)
        {
            keys.Add(new NSString("checkedInDate"));
            values.Add(new NSString(checkedIn.UtcDateTime.ToString("O")));
        }

        return NSDictionary<NSString, NSObject>.FromObjectsAndKeys(
            values.ToArray(), keys.ToArray()
        );
    }

    public static void SendCurrentState(WCSession? session = null)
    {
        session ??= WCSession.DefaultSession;
        if (!WCSession.IsSupported || !session.Reachable)
            return;

        try
        {
            session.SendMessage(BuildStateDict(), null, null);
        }
        catch
        {
            // watch may not be reachable
        }
    }
}
