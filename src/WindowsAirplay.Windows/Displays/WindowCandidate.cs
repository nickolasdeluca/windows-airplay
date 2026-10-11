using System;
namespace WindowsAirplay.Windows.Displays;
public sealed record WindowCandidate(IntPtr Handle,int ProcessId,string ClassName,bool Visible);
