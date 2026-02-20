using CoreLocation;
using Foundation;
using MapKit;
using UIKit;

namespace ShinyKmlRecorder;

public class CarPlayMapViewController : UIViewController, IMKMapViewDelegate
{
    MKMapView mapView = null!;
    MKPolyline? currentPolyline;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        mapView = new MKMapView(View!.Bounds)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            ShowsUserLocation = true
        };
        mapView.SetUserTrackingMode(MKUserTrackingMode.Follow, false);
        mapView.WeakDelegate = this;
        View.AddSubview(mapView);
    }

    public void UpdateRoute(IList<LogRecord> points)
    {
        // TODO: this should be a delta and ideally we aren't redrawing the line everytime
        if (currentPolyline != null)
            mapView.RemoveOverlay(currentPolyline);

        if (points.Count < 2)
            return;

        var coordinates = points
            .Select(p => new CLLocationCoordinate2D(p.Latitude, p.Longitude))
            .ToArray();

        currentPolyline = MKPolyline.FromCoordinates(coordinates);
        mapView.AddOverlay(currentPolyline);
    }

    [Export("mapView:rendererForOverlay:")]
    public MKOverlayRenderer GetRendererForOverlay(MKMapView mapView, IMKOverlay overlay)
    {
        if (overlay is MKPolyline polyline)
        {
            return new MKPolylineRenderer(polyline)
            {
                StrokeColor = UIColor.SystemBlue,
                LineWidth = 4
            };
        }
        return new MKOverlayRenderer(overlay);
    }
}
