namespace ShinyKmlRecorder.Services;

[Singleton]
public record BaseServices(
    INavigator Navigator,
    ILogService Logs
);
    
