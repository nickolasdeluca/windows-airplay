using WindowsAirplay.Core.App;
using WindowsAirplay.Core.Displays;
using WindowsAirplay.Core.Receiver;
using WindowsAirplay.Core.Settings;
using Xunit;
namespace WindowsAirplay.Tests.App;
public sealed class MainViewModelTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"airplay-model-"+Guid.NewGuid());
    private readonly FakeReceiver receiver=new();
    private readonly FakeDisplays displays=new();
    private SettingsStore Store => new(Path.Combine(directory,"settings.json"));
    private MainViewModel Create() => new(receiver,displays,Store,directory,action=>action());
    [Fact] public async Task MissingSavedMonitorRequiresReselection() { await Store.SaveAsync(new("Office","gone")); using var vm=Create(); await vm.InitializeAsync(); Assert.Null(vm.SelectedMonitor); Assert.False(vm.CanStart); Assert.Contains("monitor",vm.Status,StringComparison.OrdinalIgnoreCase); }
    [Fact] public async Task InvalidNameDisablesStart() { using var vm=Create(); await vm.InitializeAsync(); vm.SelectedMonitor=vm.Monitors[0]; vm.ReceiverName="-bad"; Assert.False(vm.StartCommand.CanExecute(null)); vm.ReceiverName="Escritório"; Assert.True(vm.CanStart); }
    [Fact] public async Task LifecycleUpdatesCommandsAndClearsPairingCode() { using var vm=Create(); await vm.InitializeAsync(); vm.SelectedMonitor=vm.Monitors[0]; await vm.StartAsync(); Assert.False(vm.CanStart); Assert.True(vm.StopCommand.CanExecute(null)); receiver.EmitPin("1234"); Assert.Equal("1234",vm.PairingCode); receiver.SetState(ReceiverState.Connected); Assert.Equal("",vm.PairingCode); await vm.StopAsync(); Assert.True(vm.CanStart); Assert.False(vm.CanStop); }
    [Fact] public async Task UnplugStopsReceivingAndRequiresReselection() { using var vm=Create(); await vm.InitializeAsync(); vm.SelectedMonitor=vm.Monitors[0]; await vm.StartAsync(); displays.Items=[]; await vm.RefreshDisplaysAsync(); Assert.Equal(ReceiverState.Stopped,receiver.State); Assert.True(displays.Restored); Assert.Null(vm.SelectedMonitor); Assert.False(vm.CanStart); Assert.Contains("disconnected",vm.Status,StringComparison.OrdinalIgnoreCase); }
    public void Dispose() { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
    private sealed class FakeDisplays : IDisplayHost {
        public MonitorDescriptor[] Items=[new("screen","Left display",new(-1920,0,1920,1080),false)]; public bool Restored;
        public IReadOnlyList<MonitorDescriptor> GetMonitors()=>Items;
        public bool TryPresent(int pid,string id)=>true;
        public void Restore()=>Restored=true;
    }
    private sealed class FakeReceiver : IReceiverSession {
        public ReceiverState State { get; private set; }
        public int? ProcessId=>State is ReceiverState.Listening or ReceiverState.Connected ? 123 : null;
        public IReadOnlyList<string> Diagnostics=>[];
        public event EventHandler? StateChanged;
        public event EventHandler<string>? PairingCodeAvailable;
        public void SetState(ReceiverState state) { State=state; StateChanged?.Invoke(this,EventArgs.Empty); }
        public void EmitPin(string pin)=>PairingCodeAvailable?.Invoke(this,pin);
        public Task StartAsync(ReceiverOptions options,CancellationToken cancellationToken=default) { SetState(ReceiverState.Listening); return Task.CompletedTask; }
        public Task StopAsync(CancellationToken cancellationToken=default) { SetState(ReceiverState.Stopped); return Task.CompletedTask; }
    }
}
