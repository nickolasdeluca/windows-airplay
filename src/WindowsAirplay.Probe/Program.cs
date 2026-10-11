using System.Diagnostics;
using WindowsAirplay.Core.Receiver;
using WindowsAirplay.Windows.Displays;
namespace WindowsAirplay.Probe;
internal static class Program
{
    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        using var cancellation=new CancellationTokenSource();
        Console.CancelKeyPress+=(_,e)=>{e.Cancel=true;cancellation.Cancel();};
        try
        {
            using var monitors=new MonitorCatalog();
            var screens=monitors.GetMonitors();
            if(args.Length==0 || args.Contains("--list-monitors"))
            {
                for(int i=0;i<screens.Count;i++) Console.WriteLine($"{i+1}. {screens[i].Name}\n   {screens[i].Bounds} {(screens[i].IsPrimary ? "(primary)" : "")}\n   ID: {screens[i].Id}");
                Console.WriteLine("Usage: WindowsAirplay.Probe --monitor <number-or-id> --runtime <directory> [--test-pattern]");
                return 0;
            }
            string GetOption(string option) { int i=Array.IndexOf(args,option); return i>=0 && i+1<args.Length ? args[i+1] : throw new ArgumentException($"Required option: {option}"); }
            var selection=GetOption("--monitor");
            var monitor=int.TryParse(selection,out int number) && number>=1 && number<=screens.Count ? screens[number-1] : screens.SingleOrDefault(x=>x.Id==selection) ?? throw new ArgumentException("Monitor not found. Run --list-monitors.");
            var runtime=Path.GetFullPath(GetOption("--runtime"));
            using var presenter=new ReceiverWindowPresenter(monitors);
            if(args.Contains("--test-pattern"))
            {
                var start=UxPlayArguments.Build(new("Test",runtime));
                start.FileName=Path.Combine(runtime,"gst-launch-1.0.exe"); start.ArgumentList.Clear();
                foreach(var arg in new[]{"videotestsrc","num-buffers=180","!","video/x-raw,width=1280,height=720,framerate=30/1","!","d3d11videosink","fullscreen-toggle-mode=none"}) start.ArgumentList.Add(arg);
                using var child=Process.Start(start) ?? throw new InvalidOperationException("Test pattern failed to start.");
                var output=child.StandardOutput.ReadToEndAsync(); var errors=child.StandardError.ReadToEndAsync();
                bool placed=false;
                try
                {
                    using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token); deadline.CancelAfter(TimeSpan.FromSeconds(20));
                    while(!child.HasExited)
                    {
                        if(presenter.TryPresent(child.Id,monitor.Id)) placed=true;
                        await Task.Delay(100,deadline.Token);
                    }
                    await Task.WhenAll(output,errors);
                    if(child.ExitCode!=0 || !placed) throw new InvalidOperationException("D3D11 test pattern failed to render on the selected monitor. Check the runtime and GPU driver.");
                    Console.WriteLine($"PASS: D3D11 video window placed at {monitor.Bounds}. AirPlay compatibility has not been tested.");
                    return 0;
                }
                finally { presenter.Restore(); if(!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); } }
            }
            var session=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WindowsAirplay","Probe");
            await using var receiver=new ReceiverSupervisor(session);
            receiver.StateChanged+=(_,_)=>Console.WriteLine($"Receiver: {receiver.State}");
            receiver.PairingCodeAvailable+=(_,code)=>Console.WriteLine($"Enter this code on your Mac: {code}");
            Console.WriteLine($"Target: {monitor.Name}. Requested format: 1920x1080 at 60 Hz; actual output depends on the Mac.");
            Console.WriteLine("Select Windows AirPlay from the Mac's AirPlay menu, then choose Extended Display. Alt+Tab back to this terminal and press Ctrl+C to stop.");
            try
            {
                await receiver.StartAsync(new("Windows AirPlay",runtime),cancellation.Token);
                var shownDiagnostics=new HashSet<string>();
                while(!cancellation.IsCancellationRequested)
                {
                    foreach(var message in receiver.Diagnostics)
                        if(shownDiagnostics.Add(message)) Console.Error.WriteLine(message);
                    if(receiver.State==ReceiverState.Faulted) throw new InvalidOperationException("Receiver stopped unexpectedly.");
                    var id=receiver.ProcessId;
                    if(id is not null) presenter.TryPresent(id.Value,monitor.Id);
                    await Task.Delay(100,cancellation.Token);
                }
            }
            finally { presenter.Restore(); await receiver.StopAsync(); foreach(var message in receiver.Diagnostics) Console.Error.WriteLine(message); }
            return 0;
        }
        catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { return 0; }
        catch(Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
}
