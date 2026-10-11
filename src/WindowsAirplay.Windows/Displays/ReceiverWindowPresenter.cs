using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsAirplay.Core.Displays;
namespace WindowsAirplay.Windows.Displays;
public sealed class ReceiverWindowPresenter(MonitorCatalog monitors) : IDisposable
{
    private Process? process;
    private IntPtr window;
    private IntPtr originalStyle;
    private NativeDisplayMethods.Rect originalBounds;
    private PixelRect? placedBounds;
    public bool IsPresenting => window!=IntPtr.Zero && Owned(window);
    public async Task PresentAsync(int processId,string monitorId,CancellationToken cancellationToken=default)
    {
        var deadline=Stopwatch.StartNew();
        while(!TryPresent(processId,monitorId))
        {
            if(deadline.Elapsed>TimeSpan.FromSeconds(15)) throw new TimeoutException("Receiver video window did not appear.");
            await Task.Delay(100,cancellationToken);
        }
    }
    public bool TryPresent(int processId,string monitorId)
    {
        var bounds=PresentationPolicy.ResolveBounds(monitors.GetMonitors(),monitorId);
        if(process is null || process.HasExited || process.Id!=processId) { Restore(); process=Process.GetProcessById(processId); }
        if(process.HasExited) return false;
        var candidates=new List<WindowCandidate>();
        NativeDisplayMethods.EnumWindows((handle,_)=>
        {
            NativeDisplayMethods.GetWindowThreadProcessId(handle,out uint owner);
            if(owner==(uint)processId)
            {
                var name=new StringBuilder(256); NativeDisplayMethods.GetClassName(handle,name,name.Capacity);
                candidates.Add(new(handle,(int)owner,name.ToString(),NativeDisplayMethods.IsWindowVisible(handle)));
            }
            return true;
        },IntPtr.Zero);
        var selected=PresentationPolicy.SelectWindow(candidates,processId);
        if(selected==IntPtr.Zero) return false;
        if(window==selected && placedBounds==bounds) return true;
        if(window!=IntPtr.Zero && window!=selected) RestoreWindow();
        if(!Owned(selected)) return false;
        if(window!=selected)
        {
            if(!NativeDisplayMethods.GetWindowRect(selected,out originalBounds)) throw new Win32Exception(Marshal.GetLastWin32Error());
            originalStyle=NativeDisplayMethods.GetWindowLongPtr(selected,-16);
            window=selected;
        }
        var borderless=new IntPtr(originalStyle.ToInt64() & ~0x00CF0000L);
        Marshal.SetLastPInvokeError(0);
        var previous=NativeDisplayMethods.SetWindowLongPtr(selected,-16,borderless);
        if(previous==IntPtr.Zero && Marshal.GetLastWin32Error()!=0) throw new Win32Exception(Marshal.GetLastWin32Error());
        if(!Owned(selected)) return false;
        if(!NativeDisplayMethods.SetWindowPos(selected,IntPtr.Zero,bounds.X,bounds.Y,bounds.Width,bounds.Height,0x70)) throw new Win32Exception(Marshal.GetLastWin32Error());
        if(!NativeDisplayMethods.GetWindowRect(selected,out var actual) || actual.Left!=bounds.X || actual.Top!=bounds.Y || actual.Right-actual.Left!=bounds.Width || actual.Bottom-actual.Top!=bounds.Height)
            throw new InvalidOperationException("Windows did not apply the selected monitor bounds.");
        placedBounds=bounds; return true;
    }
    private bool Owned(IntPtr handle)
    {
        if(process is null || process.HasExited) return false;
        NativeDisplayMethods.GetWindowThreadProcessId(handle,out uint pid);
        var name=new StringBuilder(256); NativeDisplayMethods.GetClassName(handle,name,name.Capacity);
        return pid==(uint)process.Id && name.ToString()=="GSTD3D11";
    }
    private void RestoreWindow()
    {
        if(window!=IntPtr.Zero && Owned(window))
        {
            NativeDisplayMethods.SetWindowLongPtr(window,-16,originalStyle);
            var r=originalBounds;
            NativeDisplayMethods.SetWindowPos(window,IntPtr.Zero,r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top,0x34);
        }
        window=IntPtr.Zero; placedBounds=null;
    }
    public void Restore() { RestoreWindow(); process?.Dispose(); process=null; }
    public void Dispose() => Restore();
}
