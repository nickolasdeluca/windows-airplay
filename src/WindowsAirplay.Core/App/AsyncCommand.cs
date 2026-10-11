using System.Windows.Input;
namespace WindowsAirplay.Core.App;
public sealed class AsyncCommand(Func<Task> execute,Func<bool> canExecute) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter)=>canExecute();
    public async void Execute(object? parameter) { if(CanExecute(parameter)) await execute(); }
    public void Refresh()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);
}
