using WindowsAirplay.Core.Displays;
using WindowsAirplay.Windows.Displays;
using Xunit;
namespace WindowsAirplay.Tests.Displays;
public sealed class PlacementTests
{
    private static readonly MonitorDescriptor[] Screens=[new("primary","Main",new(0,0,3840,2160),true),new("left","Side",new(-1920,-120,1920,1080),false)];
    [Fact] public void NegativeCoordinatesRemainPhysicalPixels() => Assert.Equal(new PixelRect(-1920,-120,1920,1080),PresentationPolicy.ResolveBounds(Screens,"left"));
    [Fact] public void HighResolutionBoundsAreNotRescaled() => Assert.Equal(new PixelRect(0,0,3840,2160),PresentationPolicy.ResolveBounds(Screens,"primary"));
    [Fact] public void MissingMonitorNeverFallsBackToPrimary() => Assert.Throws<InvalidOperationException>(()=>PresentationPolicy.ResolveBounds(Screens,"removed"));
}
