using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using WindowsAirplay.Core.Displays;
using WindowsAirplay.Core.Receiver;
using WindowsAirplay.Core.Settings;
namespace WindowsAirplay.Core.App;
public sealed class MainViewModel : INotifyPropertyChanged,IDisposable
{
    private readonly IReceiverSession receiver;
    private readonly IDisplayHost displays;
    private readonly SettingsStore settings;
    private readonly string runtime;
    private readonly Action<Action> dispatch;
    private CancellationTokenSource? startup;
    private bool busy,stopping,disposed;
    private string name="Windows AirPlay",status="Choose a monitor to begin.",pairingCode="",diagnostics="";
    private MonitorDescriptor? selected;
    public MainViewModel(IReceiverSession receiver,IDisplayHost displays,SettingsStore settings,string runtime,Action<Action> dispatch)
    {
        this.receiver=receiver; this.displays=displays; this.settings=settings; this.runtime=runtime; this.dispatch=dispatch;
        StartCommand=new(StartAsync,()=>CanStart); StopCommand=new(StopAsync,()=>CanStop);
        receiver.StateChanged+=OnStateChanged; receiver.PairingCodeAvailable+=OnPairingCode;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? AttentionRequested;
    public ObservableCollection<MonitorDescriptor> Monitors { get; }=[];
    public AsyncCommand StartCommand { get; }
    public AsyncCommand StopCommand { get; }
    public string ReceiverName { get=>name; set { if(name==value)return; name=value; Changed(); RefreshCommands(); } }
    public MonitorDescriptor? SelectedMonitor { get=>selected; set { selected=value; Changed(); RefreshCommands(); } }
    public string Status { get=>status; private set { status=value; Changed(); } }
    public string PairingCode { get=>pairingCode; private set { pairingCode=value; Changed(); Changed(nameof(HasPairingCode)); } }
    public bool HasPairingCode=>PairingCode.Length>0;
    public string Diagnostics { get=>diagnostics; private set { diagnostics=value; Changed(); } }
    public bool IsRunning=>receiver.State is ReceiverState.Starting or ReceiverState.Listening or ReceiverState.Connected or ReceiverState.Stopping;
    public bool CanEdit=>!busy && !stopping && !IsRunning;
    public bool CanStart=>!disposed && CanEdit && selected is not null && Monitors.Any(x=>x.Id==selected.Id) && !string.IsNullOrWhiteSpace(name) && !name.StartsWith('-') && !name.Any(char.IsControl) && Encoding.UTF8.GetByteCount(name)<=50;
    public bool CanStop=>!disposed && !stopping && (busy || IsRunning);
    public async Task InitializeAsync()
    {
        var saved=await settings.LoadAsync(); ReceiverName=saved.ReceiverName;
        await RefreshDisplaysAsync(); SelectedMonitor=Monitors.FirstOrDefault(x=>x.Id==saved.MonitorId);
        if(settings.Diagnostic is not null) Status=settings.Diagnostic;
        else if(saved.MonitorId is not null && SelectedMonitor is null) Status="Your saved monitor is unavailable. Select a monitor to continue.";
    }
    public async Task StartAsync()
    {
        if(!CanStart)return;
        busy=true; startup=new(); var token=startup.Token; RefreshCommands(); PairingCode=""; Diagnostics="";
        try {
            await settings.SaveAsync(new(ReceiverName,SelectedMonitor!.Id),token);
            await receiver.StartAsync(new(ReceiverName,runtime),token);
        } catch(OperationCanceledException) { Status="Stopped."; }
        catch(Exception error) { Status=FriendlyError(error); AttentionRequested?.Invoke(this,EventArgs.Empty); }
        finally { busy=false; startup.Dispose(); startup=null; RefreshCommands(); }
    }
    public async Task StopAsync()
    {
        if(stopping)return;
        stopping=true; startup?.Cancel(); RefreshCommands();
        try { try { displays.Restore(); } finally { await receiver.StopAsync(); } Status="Stopped."; }
        catch(Exception error) { Status=FriendlyError(error); }
        finally { PairingCode=""; stopping=false; RefreshCommands(); }
    }
    public async Task RefreshDisplaysAsync()
    {
        if(disposed)return;
        try {
            var id=SelectedMonitor?.Id; var current=displays.GetMonitors();
            Monitors.Clear(); foreach(var monitor in current) Monitors.Add(monitor);
            SelectedMonitor=current.FirstOrDefault(x=>x.Id==id);
            if(id is not null && SelectedMonitor is null) {
                await StopAsync(); Status="The selected monitor disconnected. Choose another monitor.";
                AttentionRequested?.Invoke(this,EventArgs.Empty);
            }
        } catch(Exception error) { await StopAsync(); Status=FriendlyError(error); }
    }
    public async Task TickAsync()
    {
        if(disposed || stopping)return;
        Diagnostics=string.Join(Environment.NewLine,receiver.Diagnostics.Distinct());
        try { if(receiver.ProcessId is int pid && SelectedMonitor is not null) displays.TryPresent(pid,SelectedMonitor.Id); }
        catch(Exception error) { await StopAsync(); Status=FriendlyError(error); AttentionRequested?.Invoke(this,EventArgs.Empty); }
    }
    private void OnStateChanged(object? sender,EventArgs args)=>dispatch(()=>{
        if(disposed)return;
        Status=receiver.State switch {
            ReceiverState.Starting=>"Starting receiver…",
            ReceiverState.Listening=>"Ready — select this receiver on your Mac.",
            ReceiverState.Connected=>"Connected — your Mac is streaming.",
            ReceiverState.Stopping=>"Stopping…",
            ReceiverState.Faulted=>"Receiver stopped unexpectedly. Check diagnostics and try again.",
            _=>"Stopped."
        };
        if(receiver.State is ReceiverState.Connected or ReceiverState.Stopped or ReceiverState.Faulted) PairingCode="";
        RefreshCommands();
    });
    private void OnPairingCode(object? sender,string code)=>dispatch(()=>{ if(!disposed && receiver.State is ReceiverState.Listening or ReceiverState.Starting) { PairingCode=code; AttentionRequested?.Invoke(this,EventArgs.Empty); } });
    private static string FriendlyError(Exception error)=>error switch {
        FileNotFoundException=>"Receiver files are missing. Prepare the receiver runtime and try again.",
        TimeoutException=>"Receiver did not become ready. Check Bonjour, network access, and receiver diagnostics.",
        IOException=>"A file or device is unavailable. Check the receiver runtime and selected monitor.",
        UnauthorizedAccessException=>"Windows denied access. Check permissions for the app settings and receiver files.",
        _=>"Unable to start or display the receiver. Check the selected monitor, Bonjour, and whether another receiver is running."
    };
    private void RefreshCommands() { Changed(nameof(CanStart)); Changed(nameof(CanStop)); Changed(nameof(CanEdit)); Changed(nameof(IsRunning)); StartCommand.Refresh(); StopCommand.Refresh(); }
    private void Changed([CallerMemberName]string? property=null)=>PropertyChanged?.Invoke(this,new(property));
    public void Dispose() { disposed=true; startup?.Cancel(); receiver.StateChanged-=OnStateChanged; receiver.PairingCodeAvailable-=OnPairingCode; }
}
