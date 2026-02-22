using Foundation;
using ObjCRuntime;
using System.Runtime.InteropServices;

namespace ShinyKmlRecorder.Services.Impl;

public partial class LogService
{
    const string AppGroupId = "group.org.shinylib.kmlrecorder";
    const string IsRecordingKey = "isRecording";
    const string CheckedInDateKey = "checkedInDate";

    partial void SyncWidgetState()
    {
        var defaults = new NSUserDefaults(AppGroupId, NSUserDefaultsType.SuiteName);
        var isRecording = this.DateCheckedIn != null;
        defaults.SetBool(isRecording, IsRecordingKey);

        if (isRecording && this.DateCheckedIn is { } checkedIn)
            defaults.SetString(checkedIn.UtcDateTime.ToString("O"), CheckedInDateKey);
        else
            defaults.RemoveObject(CheckedInDateKey);

        defaults.Synchronize();
        ReloadWidgetTimelines();
    }

    static void ReloadWidgetTimelines()
    {
        // WidgetKit is not bound in .NET for iOS - use ObjC runtime
        var widgetCenterClass = Class.GetHandle("WidgetCenter");
        if (widgetCenterClass == IntPtr.Zero)
            return;

        var shared = objc_msgSend_ptr(widgetCenterClass, Selector.GetHandle("sharedCenter"));
        if (shared == IntPtr.Zero)
            return;

        objc_msgSend_void(shared, Selector.GetHandle("reloadAllTimelines"));
    }

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr objc_msgSend_ptr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern void objc_msgSend_void(IntPtr receiver, IntPtr selector);
}
