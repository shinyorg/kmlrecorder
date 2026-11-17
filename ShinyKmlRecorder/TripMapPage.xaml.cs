using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;
using MBrush = Mapsui.Styles.Brush;
using MColor = Mapsui.Styles.Color;

namespace ShinyKmlRecorder;

public partial class TripMapPage : ContentPage
{
    // OpenStreetMap's tile usage policy requires a User-Agent that identifies the app
    const string UserAgent = "ShinyKmlRecorder/1.0 (+https://github.com/shinyorg/kmlrecorder)";

    static readonly MColor RouteColor = new(126, 87, 194);   // app primary
    static readonly MColor StartColor = new(46, 160, 67);
    static readonly MColor EndColor = new(229, 57, 53);

    // features carry their own styles, so the layer adds none
    readonly MemoryLayer routeLayer = new() { Name = "Route", Style = null };


    public TripMapPage()
    {
        this.InitializeComponent();

        // the tile layer brings the "© OpenStreetMap contributors" attribution with it
        this.mapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer(UserAgent));
        this.mapControl.Map.Layers.Add(this.routeLayer);
    }


    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (this.BindingContext is TripMapViewModel vm)
        {
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TripMapViewModel.Points) && vm.Points is { Count: > 0 } points)
                    MainThread.BeginInvokeOnMainThread(() => this.ShowRoute(points));
            };
        }
    }


    void ShowRoute(IList<LogRecord> points)
    {
        var coordinates = points
            .Select(x => SphericalMercator.FromLonLat(x.Longitude, x.Latitude))
            .Select(x => new Coordinate(x.x, x.y))
            .ToArray();

        var features = new List<IFeature>();
        if (coordinates.Length > 1)
        {
            var route = new GeometryFeature(new LineString(coordinates));
            route.Styles.Add(new VectorStyle { Line = new Pen(RouteColor, 5) });
            features.Add(route);
        }
        features.Add(Marker(coordinates[0], StartColor));
        features.Add(Marker(coordinates[^1], EndColor));

        this.routeLayer.Features = features;
        this.routeLayer.DataHasChanged();

        var extent = new MRect(
            coordinates.Min(x => x.X),
            coordinates.Min(x => x.Y),
            coordinates.Max(x => x.X),
            coordinates.Max(x => x.Y)
        );

        // a trip that never moved is a point, which has no box to fit - give it a few hundred metres
        var padding = Math.Max(Math.Max(extent.Width, extent.Height) * 0.15, 250);
        this.mapControl.Map.Navigator.ZoomToBox(extent.Grow(padding), MBoxFit.Fit);
    }


    static PointFeature Marker(Coordinate coordinate, MColor color)
    {
        var feature = new PointFeature(coordinate.X, coordinate.Y);
        feature.Styles.Add(new SymbolStyle
        {
            SymbolScale = 0.6,
            Fill = new MBrush(color),
            Outline = new Pen(MColor.White, 3)
        });
        return feature;
    }
}
