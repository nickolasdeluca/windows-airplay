using System;
using System.Collections.Generic;
using System.Linq;
using WindowsAirplay.Core.Displays;
namespace WindowsAirplay.Windows.Displays;
public static class PresentationPolicy
{
    public static IntPtr SelectWindow(IReadOnlyList<WindowCandidate> windows,int processId)
    {
        var candidates=windows.Where(w=>w.ProcessId==processId && w.Visible && w.ClassName=="GSTD3D11").ToArray();
        if(candidates.Length>1) throw new InvalidOperationException("Multiple receiver video windows found; stop and restart the receiver.");
        return candidates.SingleOrDefault()?.Handle ?? IntPtr.Zero;
    }
    public static PixelRect ResolveBounds(IReadOnlyList<MonitorDescriptor> monitors,string id) =>
        monitors.SingleOrDefault(m=>m.Id==id)?.Bounds ?? throw new InvalidOperationException("Selected monitor is unavailable. Stop and select another monitor.");
}
