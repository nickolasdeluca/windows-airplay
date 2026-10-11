using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using WindowsAirplay.Core.Displays;
namespace WindowsAirplay.Windows.Displays;
public sealed class MonitorCatalog : IDisposable
{
    public event EventHandler? Changed;
    public MonitorCatalog() => SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
    private void OnDisplayChanged(object? sender,EventArgs e) => Changed?.Invoke(this,EventArgs.Empty);
    public IReadOnlyList<MonitorDescriptor> GetMonitors()
    {
        var result=new List<MonitorDescriptor>();
        NativeDisplayMethods.MonitorCallback callback=(IntPtr monitor,IntPtr dc,ref NativeDisplayMethods.Rect ignored,IntPtr data)=>
        {
            var info=new NativeDisplayMethods.MonitorInfo { Size=Marshal.SizeOf<NativeDisplayMethods.MonitorInfo>(),Device="" };
            if(!NativeDisplayMethods.GetMonitorInfo(monitor,ref info)) return true;
            var device=new NativeDisplayMethods.DisplayDevice { Size=Marshal.SizeOf<NativeDisplayMethods.DisplayDevice>(),Name="",Description="",Id="",Key="" };
            // EDD_GET_DEVICE_INTERFACE_NAME provides a persistent monitor interface path.
            if(!NativeDisplayMethods.EnumDisplayDevices(info.Device,0,ref device,1) || string.IsNullOrEmpty(device.Id))
                throw new InvalidOperationException("Windows did not provide a stable monitor identity.");
            var r=info.Bounds;
            result.Add(new(device.Id,$"{info.Device} — {device.Description}",new(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top),(info.Flags&1)!=0));
            return true;
        };
        if(!NativeDisplayMethods.EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,callback,IntPtr.Zero)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return result;
    }
    public void Dispose() => SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
}
