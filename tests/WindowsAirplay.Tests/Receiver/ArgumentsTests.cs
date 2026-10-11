using WindowsAirplay.Core.Receiver;
using Xunit;
namespace WindowsAirplay.Tests.Receiver;
public sealed class ArgumentsTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "airplay-tests-" + Guid.NewGuid());
    public ArgumentsTests() { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "uxplay.exe"), "fixture"); }
    [Fact] public void UnicodeNameRemainsOneArgument()
    {
        var result = UxPlayArguments.Build(new("Nick's Mac — escritório", directory));
        Assert.False(result.UseShellExecute);
        Assert.Contains("Nick's Mac — escritório", result.ArgumentList);
        Assert.Contains("1920x1080@60", result.ArgumentList);
        Assert.Contains("-pw", result.ArgumentList);
        Assert.DoesNotContain("-d", result.ArgumentList);
    }
    [Theory] [InlineData(1023)] [InlineData(65534)]
    public void PortRangeMustFitThreePorts(int port) => Assert.Throws<ArgumentOutOfRangeException>(() => UxPlayArguments.Build(new("AirPlay", directory, port)));
    [Theory] [InlineData("")] [InlineData("bad\nname")] [InlineData("-pw")]
    public void InvalidNameIsRejected(string name) => Assert.Throws<ArgumentException>(() => UxPlayArguments.Build(new(name, directory)));
    [Fact] public void MissingExecutableFailsBeforeLaunch() => Assert.Throws<FileNotFoundException>(() => UxPlayArguments.Build(new("AirPlay", Path.Combine(directory,"missing"))));
    public void Dispose() => Directory.Delete(directory, true);
}
