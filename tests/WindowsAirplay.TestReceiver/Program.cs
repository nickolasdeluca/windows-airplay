using System.Diagnostics;
using WindowsAirplay.Core.Receiver;
if(args.Length>0 && args[0]=="--leaf") { await Task.Delay(Timeout.Infinite); return 0; }
if(args.Length==3 && args[0]=="--supervise") {
    await using var owner=new ReceiverSupervisor(args[2]);
    await owner.StartAsync(new("normal",args[1]));
    Console.WriteLine(owner.ProcessId); Console.Out.Flush();
    await Task.Delay(Timeout.Infinite); return 0;
}
var index = Array.IndexOf(args, "-n");
var mode = index >= 0 ? args[index + 1] : "normal";
if(mode=="descendant") {
    var start=new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute=false,CreateNoWindow=true };
    start.ArgumentList.Add("--leaf");
    using var child=Process.Start(start)!;
    await File.WriteAllTextAsync(Path.Combine(Environment.CurrentDirectory,"descendant.pid"),child.Id.ToString());
}
if (mode == "crash") return 7;
if (mode != "stall") { Console.WriteLine("WINDOWSAIRPLAY_READY"); Console.Out.Flush(); }
if (mode == "stream") Console.WriteLine("Begin streaming to GStreamer video pipeline");
if (mode == "flood") { for (var i = 0; i < 1000; i++) Console.Error.WriteLine("GStreamer error (video): password secret"); Console.Write(new string('x',100000)); Console.WriteLine(); }
if (mode == "race") { await Task.Delay(200); for(var i=0;i<300;i++) Console.WriteLine("WINDOWSAIRPLAY_READY"); return 7; }
if (mode == "crash-later") { await Task.Delay(250); return 7; }
await Task.Delay(Timeout.Infinite);
return 0;
