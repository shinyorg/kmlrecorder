using System.Net.Sockets;
using Android.Content;
using Android.Net;

namespace ShinyKmlRecorder.Services.Impl;


public partial class ObdManager
{
    partial void BindToWifiNetwork(bool bind)
    {
        var cm = (ConnectivityManager?)Platform.AppContext.GetSystemService(Context.ConnectivityService);
        if (cm == null)
            return;

        if (!bind)
        {
            cm.BindProcessToNetwork(null);
            return;
        }

        // OBD adapter access points have no internet, so Android routes sockets over cellular unless we bind
        var wifi = FindWifiNetwork(cm);
        if (wifi != null)
            cm.BindProcessToNetwork(wifi);
    }


    partial void FindWifiGateway(ref string? gateway)
    {
        var cm = (ConnectivityManager?)Platform.AppContext.GetSystemService(Context.ConnectivityService);
        var wifi = cm == null ? null : FindWifiNetwork(cm);
        var routes = wifi == null ? null : cm!.GetLinkProperties(wifi)?.Routes;

        gateway = routes?
            .Where(x => x.IsDefaultRoute && x.Gateway?.HostAddress != null)
            .Select(x => System.Net.IPAddress.TryParse(x.Gateway!.HostAddress, out var ip) ? ip : null)
            .FirstOrDefault(x => x?.AddressFamily == AddressFamily.InterNetwork)?
            .ToString();
    }


    static Network? FindWifiNetwork(ConnectivityManager cm) => cm
        .GetAllNetworks()
        .FirstOrDefault(x => cm.GetNetworkCapabilities(x)?.HasTransport(Android.Net.TransportType.Wifi) == true);
}
