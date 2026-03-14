namespace ShinyKmlRecorder.Services;

[Singleton]
public record BaseServices(
    INavigator Navigator,
    IDialogs Dialogs,
    ILogService Logs
);
    
