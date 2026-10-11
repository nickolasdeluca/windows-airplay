using WindowsAirplay.Core.Receiver;
using Xunit;
namespace WindowsAirplay.Tests.Receiver;
public sealed class DiagnosticsTests
{
    [Theory] [InlineData("password = 654321")] [InlineData("123456")] [InlineData("registered new client: DeviceID secret")] [InlineData("unexpected secret material")]
    public void UnrecognizedOrSensitiveOutputIsNeverPersisted(string line) => Assert.Equal("", ReceiverDiagnostics.Sanitize(line));
    [Theory]
    [InlineData("Client authentication success", "The Mac password was accepted.")]
    [InlineData("Client authentication failure", "The Mac authentication attempt failed. Retry with the displayed code.")]
    [InlineData("authentication rejected (nonce mismatch) secret1 secret2", "Authentication challenge changed before the Mac response arrived. Reconnect and retry.")]
    public void AuthenticationStagesProduceSafeMessages(string input,string expected) => Assert.Equal(expected,ReceiverDiagnostics.Sanitize(input));
    [Fact] public void MissingBonjourHasActionableSafeMessage() => Assert.Contains("Bonjour", ReceiverDiagnostics.Sanitize("Error: No DNS-SD Server found"));
}
