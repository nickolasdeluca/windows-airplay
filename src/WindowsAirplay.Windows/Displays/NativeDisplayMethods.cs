using System;
using System.Runtime.InteropServices;
using System.Text;
namespace WindowsAirplay.Windows.Displays;
internal static class NativeDisplayMethods
{
    internal delegate bool MonitorCallback(IntPtr monitor,IntPtr dc,ref Rect rect,IntPtr data);
    internal delegate bool WindowCallback(IntPtr window,IntPtr data);
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] internal struct MonitorInfo { public int Size; public Rect Bounds,Work; public uint Flags; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Device; }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] internal struct DisplayDevice { public int Size; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Name; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string Description; public uint Flags; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string Id; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string Key; }
    [DllImport("user32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,MonitorCallback callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool EnumDisplayDevices(string? device,uint index,ref DisplayDevice info,uint flags);
    [DllImport("user32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool EnumWindows(WindowCallback callback,IntPtr data);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetClassName(IntPtr window,StringBuilder name,int max);
    [DllImport("user32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW",SetLastError=true)] internal static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)] internal static extern IntPtr SetWindowLongPtr(IntPtr window,int index,IntPtr value);
    [DllImport("user32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
