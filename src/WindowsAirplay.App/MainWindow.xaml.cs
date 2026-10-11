using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WindowsAirplay.Core.App;
using WindowsAirplay.Core.Receiver;
using WindowsAirplay.Core.Settings;
using WindowsAirplay.Windows.Displays;
namespace WindowsAirplay.App;
public partial class MainWindow : Window
{
    private readonly MonitorCatalog catalog=new();
    private readonly ReceiverWindowPresenter presenter;
    private readonly ReceiverSupervisor receiver;
    private readonly MainViewModel model;
    private readonly TrayController tray;
    private readonly DispatcherTimer timer=new() { Interval=TimeSpan.FromMilliseconds(200) };
    private bool exiting,ticking;
    public MainWindow(string runtime)
    {
        InitializeComponent();
        var local=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WindowsAirplay");
        receiver=new ReceiverSupervisor(Path.Combine(local,"Receiver")); presenter=new(catalog);
        model=new(receiver,new DisplayHost(catalog,presenter),new SettingsStore(Path.Combine(local,"settings.json")),runtime,action=>Dispatcher.BeginInvoke(action));
        DataContext=model;
        tray=new(ShowController,()=>model.StopAsync(),ExitAsync); tray.Update(false);
        model.PropertyChanged+=OnModelChanged; model.AttentionRequested+=OnAttention;
        catalog.Changed+=OnDisplaysChanged;
        Loaded+=async (_,_)=>{ try { await model.InitializeAsync(); timer.Start(); } catch(Exception) { MessageBox.Show(this,"Unable to load displays or settings. Check Windows display settings and reopen the app.","Windows AirPlay"); } };
        timer.Tick+=OnTick; Closing+=OnClosing;
    }
    private void OnModelChanged(object? sender,PropertyChangedEventArgs e)=>tray.Update(model.CanStop);
    private void OnAttention(object? sender,EventArgs e)=>ShowController();
    private void OnDisplaysChanged(object? sender,EventArgs e)=>Dispatcher.BeginInvoke(async ()=>{if(!exiting)await model.RefreshDisplaysAsync();});
    private async void OnTick(object? sender,EventArgs e) { if(ticking || exiting)return; ticking=true; try { await model.TickAsync(); } finally { ticking=false; } }
    private void ShowController() { if(exiting)return; Show(); WindowState=WindowState.Normal; Activate(); }
    private void OnClosing(object? sender,CancelEventArgs e) { if(exiting)return; e.Cancel=true; if(model.CanStop) {Hide();tray.NotifyRunning();} else _=ExitAsync(); }
    private async void ExitClicked(object sender,RoutedEventArgs e)=>await ExitAsync();
    private async Task ExitAsync()
    {
        if(exiting)return; exiting=true; timer.Stop(); IsEnabled=false;
        try { await model.StopAsync(); await receiver.DisposeAsync(); }
        finally { catalog.Changed-=OnDisplaysChanged; model.PropertyChanged-=OnModelChanged; model.AttentionRequested-=OnAttention; model.Dispose(); presenter.Dispose(); catalog.Dispose(); tray.Dispose(); Application.Current.Shutdown(); }
    }
}
