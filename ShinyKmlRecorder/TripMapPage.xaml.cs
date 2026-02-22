using Microsoft.Maui.Controls.Maps;

namespace ShinyKmlRecorder;

public partial class TripMapPage : ContentPage
{
    public TripMapPage()
    {
        this.InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (this.BindingContext is TripMapViewModel vm)
        {
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TripMapViewModel.Route) && vm.Route != null)
                {
                    map.MapElements.Clear();
                    map.MapElements.Add(vm.Route);
                }
                if (e.PropertyName == nameof(TripMapViewModel.Region) && vm.Region != null)
                {
                    map.MoveToRegion(vm.Region);
                }
            };
        }
    }
}
