var index = Array.IndexOf(args, "-n");
var mode = index >= 0 ? args[index + 1] : "normal";
if (mode == "crash") return 7;
if (mode != "stall") { Console.WriteLine("WINDOWSAIRPLAY_READY"); Console.Out.Flush(); }
if (mode == "stream") Console.WriteLine("Begin streaming to GStreamer video pipeline");
if (mode == "flood") { for (var i = 0; i < 1000; i++) Console.Error.WriteLine("GStreamer error (video): password secret"); Console.Write(new string('x',100000)); Console.WriteLine(); }
if (mode == "race") { await Task.Delay(200); for(var i=0;i<300;i++) Console.WriteLine("WINDOWSAIRPLAY_READY"); return 7; }
if (mode == "crash-later") { await Task.Delay(250); return 7; }
await Task.Delay(Timeout.Infinite);
return 0;
