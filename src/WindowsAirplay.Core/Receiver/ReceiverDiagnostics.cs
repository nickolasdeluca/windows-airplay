namespace WindowsAirplay.Core.Receiver;
public static class ReceiverDiagnostics
{
    public static string Sanitize(string line)
    {
        if (line.Contains("No DNS-SD Server", StringComparison.OrdinalIgnoreCase) || line.Contains("dnssd_register", StringComparison.OrdinalIgnoreCase))
            return "Bonjour discovery failed. Check that Bonjour Service is running and the private network allows discovery.";
        if (line.Contains("GStreamer error", StringComparison.Ordinal)) return "Video rendering failed. Check the receiver runtime and GPU driver.";
        if (line.Contains("authentication failure", StringComparison.OrdinalIgnoreCase)) return "The Mac authentication attempt failed. Retry with the displayed code.";
        return "";
    }
}
