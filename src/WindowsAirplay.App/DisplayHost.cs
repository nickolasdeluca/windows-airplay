using System.Collections.Generic;
using WindowsAirplay.Core.Displays;
using WindowsAirplay.Windows.Displays;
namespace WindowsAirplay.App;
internal sealed class DisplayHost(MonitorCatalog catalog,ReceiverWindowPresenter presenter) : IDisplayHost
{
    public IReadOnlyList<MonitorDescriptor> GetMonitors()=>catalog.GetMonitors();
    public bool TryPresent(int processId,string monitorId)=>presenter.TryPresent(processId,monitorId);
    public void Restore()=>presenter.Restore();
}
