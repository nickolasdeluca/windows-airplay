using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace WindowsAirplay.App;
internal sealed class TrayController : IDisposable
{
    private readonly NotifyIcon icon;
    private readonly ContextMenuStrip menu=new();
    private readonly Icon artwork=(Icon)SystemIcons.Application.Clone();
    private readonly ToolStripMenuItem stop;
    public TrayController(Action open,Func<Task> stopAction,Func<Task> exit)
    {
        menu.Items.Add("Open",null,(_,_)=>open());
        stop=new ToolStripMenuItem("Stop",null,async (_,_)=>await stopAction()); menu.Items.Add(stop);
        menu.Items.Add("Exit",null,async (_,_)=>await exit());
        icon=new NotifyIcon { Icon=artwork,Text="Windows AirPlay",ContextMenuStrip=menu,Visible=true };
        icon.DoubleClick+=(_,_)=>open();
    }
    public void Update(bool canStop)=>stop.Enabled=canStop;
    public void NotifyRunning()=>icon.ShowBalloonTip(3000,"Windows AirPlay is still receiving","Use this icon to Open, Stop, or Exit.",ToolTipIcon.Info);
    public void Dispose() { icon.Visible=false; icon.Dispose(); menu.Dispose(); artwork.Dispose(); }
}
