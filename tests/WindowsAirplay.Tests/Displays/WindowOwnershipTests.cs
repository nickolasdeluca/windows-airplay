using WindowsAirplay.Windows.Displays;
using Xunit;
namespace WindowsAirplay.Tests.Displays;
public sealed class WindowOwnershipTests
{
    [Fact] public void OnlyVisibleD3dWindowOwnedByReceiverIsSelected()
    {
        WindowCandidate[] windows=[new((nint)1,42,"GSTD3D11",true),new((nint)2,99,"GSTD3D11",true),new((nint)3,42,"OtherWindow",true),new((nint)4,42,"GSTD3D11",false)];
        Assert.Equal((nint)1,PresentationPolicy.SelectWindow(windows,42));
    }
    [Fact] public void AmbiguousVideoWindowsAreRejected() => Assert.Throws<InvalidOperationException>(()=>PresentationPolicy.SelectWindow([new((nint)1,42,"GSTD3D11",true),new((nint)2,42,"GSTD3D11",true)],42));
    [Fact] public void ForeignWindowsAreNeverSelected() => Assert.Equal(nint.Zero,PresentationPolicy.SelectWindow([new((nint)1,99,"GSTD3D11",true)],42));
}
