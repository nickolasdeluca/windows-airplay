using System;
using System.IO;
using System.Threading;
using System.Windows;
namespace WindowsAirplay.App;
public partial class App : Application
{
    private Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instance=new Mutex(true,@"LocalWindowsAirplay.Controller",out bool created);
        if(!created) { MessageBox.Show("Windows AirPlay is already open. Use its notification-area icon to open the controller.","Windows AirPlay"); instance.Dispose(); instance=null; Shutdown(); return; }
        try {
            string runtime=Path.Combine(AppContext.BaseDirectory,"receiver");
            int option=Array.IndexOf(e.Args,"--runtime");
            if(option>=0 && option+1<e.Args.Length) runtime=Path.GetFullPath(e.Args[option+1]);
            else if(!Directory.Exists(runtime)) {
                var directory=new DirectoryInfo(AppContext.BaseDirectory);
                while(directory is not null) { var candidate=Path.Combine(directory.FullName,"artifacts","receiver"); if(Directory.Exists(candidate)) {runtime=candidate;break;} directory=directory.Parent; }
            }
            MainWindow=new MainWindow(runtime); MainWindow.Show();
        } catch(Exception) { MessageBox.Show("The controller could not open. Check access to the app folder and your local settings.","Windows AirPlay"); Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e) { if(instance is not null) { instance.ReleaseMutex(); instance.Dispose(); } base.OnExit(e); }
}
