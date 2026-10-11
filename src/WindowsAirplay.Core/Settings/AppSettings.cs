namespace WindowsAirplay.Core.Settings;
public sealed record AppSettings(string ReceiverName,string? MonitorId)
{
    public static AppSettings Default { get; }=new("Windows AirPlay",null);
}
