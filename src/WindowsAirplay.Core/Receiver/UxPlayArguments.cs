using System.Diagnostics;
using System.Globalization;
using System.Text;
namespace WindowsAirplay.Core.Receiver;
public static class UxPlayArguments
{
    public static ProcessStartInfo Build(ReceiverOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Name) || options.Name.StartsWith('-') || options.Name.Any(char.IsControl) || Encoding.UTF8.GetByteCount(options.Name) > 50)
            throw new ArgumentException("Choose a receiver name of 1–50 UTF-8 bytes, without control characters or an initial dash.", nameof(options));
        if (options.PortBase is < 1024 or > 65533) throw new ArgumentOutOfRangeException(nameof(options), "Three consecutive ports must fit in 1024–65535.");
        if (options.Width is < 320 or > 7680 || options.Height is < 240 or > 4320 || options.RefreshRate is < 1 or > 120)
            throw new ArgumentOutOfRangeException(nameof(options), "Invalid requested display format.");
        var directory = Path.GetFullPath(options.RuntimeDirectory);
        var executable = Path.Combine(directory, "uxplay.exe");
        if (!File.Exists(executable)) throw new FileNotFoundException("Missing uxplay.exe. Run scripts/Prepare-Receiver.ps1.", executable);
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        string[] args = ["-n", options.Name, "-nh", "-s", FormattableString.Invariant($"{options.Width}x{options.Height}@{options.RefreshRate}"), "-p", options.PortBase.ToString(CultureInfo.InvariantCulture), "-vs", "d3d11videosink fullscreen-toggle-mode=none", "-as", "0", "-vsync", "no", "-pw", "-nofreeze"];
        foreach (var arg in args) start.ArgumentList.Add(arg);
        start.Environment["PATH"] = directory + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        start.Environment["GST_PLUGIN_SYSTEM_PATH_1_0"] = Path.Combine(directory,"plugins");
        start.Environment["GST_PLUGIN_PATH_1_0"] = "";
        start.Environment["GST_PLUGIN_SCANNER"] = Path.Combine(directory,"gst-plugin-scanner.exe");
        return start;
    }
}
