using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
namespace WindowsAirplay.Core.Receiver;
public sealed class ReceiverSupervisor(string sessionDirectory) : IAsyncDisposable,IReceiverSession
{
    private readonly SemaphoreSlim gate = new(1,1);
    private readonly object stateLock = new();
    private readonly Queue<string> diagnostics = new();
    private volatile Session? active;
    private volatile ReceiverState state;
    private bool disposed;
    public ReceiverState State => state;
    public int? ProcessId { get { lock(stateLock) { var s=active; return s is not null && s.Started && !s.Process.HasExited ? s.Process.Id : null; } } }
    public IReadOnlyList<string> Diagnostics { get { lock(diagnostics) return diagnostics.ToArray(); } }
    public string SessionDirectory { get; } = Path.GetFullPath(sessionDirectory);
    public event EventHandler? StateChanged;
    public event EventHandler<string>? PairingCodeAvailable;
    public async Task StartAsync(ReceiverOptions options, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            if (state is ReceiverState.Starting or ReceiverState.Listening or ReceiverState.Connected or ReceiverState.Stopping) throw new InvalidOperationException("Receiver is already running.");
            await CleanupAsync();
            var start=UxPlayArguments.Build(options);
            Directory.CreateDirectory(SessionDirectory);
            var config=Path.Combine(SessionDirectory,"uxplayrc");
            await File.WriteAllTextAsync(config,"# Managed receiver configuration\n",cancellationToken);
            start.WorkingDirectory=SessionDirectory;
            start.Environment["UXPLAYRC"]=config;
            start.Environment["GST_REGISTRY"]=Path.Combine(SessionDirectory,"gstreamer-registry.bin");
            start.ArgumentList.Add("-key"); start.ArgumentList.Add(Path.Combine(SessionDirectory,"receiver.key"));
            lock(diagnostics) diagnostics.Clear();
            SetState(ReceiverState.Starting);
            var session=new Session(new Process { StartInfo=start },new WindowsJob());
            lock(stateLock) active=session;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if(!session.Process.Start()) throw new InvalidOperationException("Receiver did not start.");
                session.Started=true;
                session.Job.Assign(session.Process);
                session.Output=PumpAsync(session.Process.StandardOutput,session);
                session.Error=PumpAsync(session.Process.StandardError,session);
                session.Exit=ObserveExitAsync(session);
                await session.Ready.Task.WaitAsync(TimeSpan.FromSeconds(60),cancellationToken);
                if(session.Process.HasExited) throw new InvalidOperationException("Receiver exited during startup.");
            }
            catch
            {
                await CleanupAsync();
                SetState(cancellationToken.IsCancellationRequested ? ReceiverState.Stopped : ReceiverState.Faulted);
                throw;
            }
        }
        finally { gate.Release(); }
    }
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try { if(active is not null) SetState(ReceiverState.Stopping); await CleanupAsync(); SetState(ReceiverState.Stopped); }
        finally { gate.Release(); }
    }
    private async Task CleanupAsync()
    {
        Session? session;
        lock(stateLock) { session=active; active=null; }
        if(session is null) return;
        session.Job.Dispose(); // Kill all owned children, including GStreamer's scanner.
        if(session.Started)
        {
            if(!session.Process.HasExited) session.Process.Kill(entireProcessTree:true);
            await session.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await Task.WhenAll(session.Output,session.Error,session.Exit).WaitAsync(TimeSpan.FromSeconds(5));
        }
        session.Process.Dispose();
    }
    private async Task ObserveExitAsync(Session session)
    {
        await session.Process.WaitForExitAsync();
        session.Ready.TrySetException(new InvalidOperationException("Receiver exited before becoming ready. Check Bonjour and native dependencies."));
        lock(stateLock)
        if(ReferenceEquals(active,session)) { AddDiagnostic("Receiver exited. Check Bonjour, ports, and native dependencies before restarting."); SetState(ReceiverState.Faulted); }
    }
    private async Task PumpAsync(StreamReader reader,Session session)
    {
        var buffer=new char[1024]; var line=new StringBuilder(); bool oversized=false;
        int count;
        while((count=await reader.ReadAsync(buffer))!=0)
        {
            for(int i=0;i<count;i++)
            {
                char value=buffer[i];
                if(value=='\n') { if(!oversized) HandleLine(line.ToString().TrimEnd('\r'),session); line.Clear(); oversized=false; }
                else if(line.Length<2048 && !oversized) line.Append(value);
                else { oversized=true; line.Clear(); }
            }
        }
        if(line.Length>0 && !oversized) HandleLine(line.ToString(),session);
    }
    private void HandleLine(string line,Session session)
    {
        lock(stateLock)
        {
            if(!ReferenceEquals(active,session) || session.Process.HasExited || state is ReceiverState.Faulted or ReceiverState.Stopping or ReceiverState.Stopped) return;
            if(line=="WINDOWSAIRPLAY_READY") { SetState(ReceiverState.Listening); session.Ready.TrySetResult(); }
            else if(line=="Begin streaming to GStreamer video pipeline") SetState(ReceiverState.Connected);
            else if(line=="GStreamer: End-Of-Stream (video)" || line.Contains("lost connection with client",StringComparison.Ordinal)) SetState(ReceiverState.Listening);
            else
            {
                var pin=Regex.Match(line,"^\\*\\*\\* CLIENT MUST NOW ENTER PIN = \"([0-9]{4,12})\" AS AIRPLAY PASSWORD$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));
                if(pin.Success) { PairingCodeAvailable?.Invoke(this,pin.Groups[1].Value); return; }
                var safe=ReceiverDiagnostics.Sanitize(line); if(safe.Length>0) AddDiagnostic(safe);
            }
        }
    }
    private void AddDiagnostic(string message) { lock(diagnostics) { if(diagnostics.Count==100) diagnostics.Dequeue(); diagnostics.Enqueue(message); } }
    private void SetState(ReceiverState value) { state=value; StateChanged?.Invoke(this,EventArgs.Empty); }
    public async ValueTask DisposeAsync() { await gate.WaitAsync(); try { if(disposed)return; disposed=true; await CleanupAsync(); SetState(ReceiverState.Stopped); } finally { gate.Release(); } }
    private sealed class Session(Process process,WindowsJob job)
    {
        public Process Process { get; }=process;
        public WindowsJob Job { get; }=job;
        public bool Started { get; set; }
        public TaskCompletionSource Ready { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Output { get; set; }=Task.CompletedTask;
        public Task Error { get; set; }=Task.CompletedTask;
        public Task Exit { get; set; }=Task.CompletedTask;
    }
}
