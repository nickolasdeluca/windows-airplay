namespace WindowsAirplay.Core.Displays;
public interface IDisplayHost
{
    IReadOnlyList<MonitorDescriptor> GetMonitors();
    bool TryPresent(int processId,string monitorId);
    void Restore();
}
