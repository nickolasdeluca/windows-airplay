using WindowsAirplay.Core.Receiver;
using Xunit;
namespace WindowsAirplay.Tests.Receiver;
public sealed class DiagnosticsTests
{
    [Theory] [InlineData("password = 654321")] [InlineData("123456")] [InlineData("registered new client: DeviceID secret")] [InlineData("unexpected secret material")]
    public void UnrecognizedOrSensitiveOutputIsNeverPersisted(string line) => Assert.Equal("", ReceiverDiagnostics.Sanitize(line));
    [Fact] public void MissingBonjourHasActionableSafeMessage() => Assert.Contains("Bonjour", ReceiverDiagnostics.Sanitize("Error: No DNS-SD Server found"));
}
