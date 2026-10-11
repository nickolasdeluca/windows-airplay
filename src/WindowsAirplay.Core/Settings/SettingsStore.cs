using System.Text.Json;
namespace WindowsAirplay.Core.Settings;
public sealed class SettingsStore(string filePath)
{
    private readonly SemaphoreSlim gate=new(1,1);
    public string FilePath { get; }=Path.GetFullPath(filePath);
    public string? Diagnostic { get; private set; }
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken=default)
    {
        await gate.WaitAsync(cancellationToken);
        try {
            Diagnostic=null;
            if(!File.Exists(FilePath)) return AppSettings.Default;
            try {
                var info=new FileInfo(FilePath);
                if(info.Length>65536) throw new JsonException();
                var settings=JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(FilePath,cancellationToken));
                if(settings is null || string.IsNullOrWhiteSpace(settings.ReceiverName) || settings.ReceiverName.Length>200 || settings.MonitorId?.Length>2048) throw new JsonException();
                return settings;
            } catch(Exception error) when(error is JsonException or IOException or UnauthorizedAccessException) {
                Diagnostic="Saved settings could not be read. Choose your monitor and receiver name again.";
                return AppSettings.Default;
            }
        } finally { gate.Release(); }
    }
    public async Task SaveAsync(AppSettings settings,CancellationToken cancellationToken=default)
    {
        await gate.WaitAsync(cancellationToken);
        var temporary=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            await File.WriteAllTextAsync(temporary,JsonSerializer.Serialize(settings),cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary,FilePath,true);
        } finally { if(File.Exists(temporary)) File.Delete(temporary); gate.Release(); }
    }
}
