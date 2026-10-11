namespace WindowsAirplay.Core.Receiver;
public interface IReceiverSession
{
    ReceiverState State { get; }
    int? ProcessId { get; }
    IReadOnlyList<string> Diagnostics { get; }
    event EventHandler? StateChanged;
    event EventHandler<string>? PairingCodeAvailable;
    Task StartAsync(ReceiverOptions options,CancellationToken cancellationToken=default);
    Task StopAsync(CancellationToken cancellationToken=default);
}
