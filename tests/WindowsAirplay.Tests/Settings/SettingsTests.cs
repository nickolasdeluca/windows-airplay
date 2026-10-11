using WindowsAirplay.Core.Settings;
using Xunit;
namespace WindowsAirplay.Tests.Settings;
public sealed class SettingsTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"airplay-settings-"+Guid.NewGuid());
    private string FilePath => Path.Combine(directory,"settings.json");
    [Fact] public async Task UnicodeAndStableMonitorRoundTrip() { var store=new SettingsStore(FilePath); var expected=new AppSettings("Escritório 🖥", "monitor-stable-id"); await store.SaveAsync(expected); Assert.Equal(expected,await store.LoadAsync()); }
    [Theory] [InlineData("broken json")] [InlineData("null")] [InlineData("{\"ReceiverName\":null}")]
    public async Task CorruptSettingsRecoverWithDiagnostic(string json) { Directory.CreateDirectory(directory); await File.WriteAllTextAsync(FilePath,json); var store=new SettingsStore(FilePath); Assert.Equal(AppSettings.Default,await store.LoadAsync()); Assert.NotNull(store.Diagnostic); }
    [Fact] public async Task CancelledSavePreservesPreviousSettings() { var store=new SettingsStore(FilePath); await store.SaveAsync(new("Office","screen")); using var cancel=new CancellationTokenSource(); cancel.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>store.SaveAsync(new("Changed","other"),cancel.Token)); Assert.Equal(new AppSettings("Office","screen"),await store.LoadAsync()); }
    public void Dispose() { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
}
