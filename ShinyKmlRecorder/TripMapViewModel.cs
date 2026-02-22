using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace ShinyKmlRecorder;

[ShellMap<TripMapPage>]
public partial class TripMapViewModel(
    BaseServices services
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] public partial Guid WorkId { get; set; }
    [ObservableProperty] public partial Polyline? Route { get; set; }
    [ObservableProperty] public partial MapSpan? Region { get; set; }
    [ObservableProperty] public partial int PointCount { get; set; }

    [RelayCommand]
    async Task Load()
    {
        var points = await services.Logs.GetTripPoints(this.WorkId);
        if (points.Count == 0)
            return;

        this.PointCount = points.Count;

        var polyline = new Polyline
        {
            StrokeColor = Colors.Blue,
            StrokeWidth = 4
        };

        foreach (var point in points)
            polyline.Geopath.Add(new Location(point.Latitude, point.Longitude));

        this.Route = polyline;

        var midPoint = points[points.Count / 2];
        var latitudes = points.Select(p => p.Latitude);
        var longitudes = points.Select(p => p.Longitude);

        var latSpan = Math.Max(0.01, (latitudes.Max() - latitudes.Min()) * 1.3);
        var lngSpan = Math.Max(0.01, (longitudes.Max() - longitudes.Min()) * 1.3);

        this.Region = new MapSpan(
            new Location(midPoint.Latitude, midPoint.Longitude),
            latSpan,
            lngSpan
        );
    }

    public void OnAppearing() => this.LoadCommand.Execute(null);
    public void OnDisappearing() { }
}
