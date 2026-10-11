using System.Diagnostics;
using WindowsAirplay.Core.Receiver;
using Xunit;
namespace WindowsAirplay.Tests.Receiver;
public sealed class SupervisorTests : IAsyncLifetime
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "airplay process " + Guid.NewGuid());
    private ReceiverSupervisor receiver = null!;
    public Task InitializeAsync()
    {
        Directory.CreateDirectory(directory);
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"fixture"))) File.Copy(file,Path.Combine(directory,Path.GetFileName(file)));
        File.Copy(Path.Combine(directory,"WindowsAirplay.TestReceiver.exe"),Path.Combine(directory,"uxplay.exe"));
        receiver = new ReceiverSupervisor(Path.Combine(directory,"session"));
        return Task.CompletedTask;
    }
    [Fact] public async Task StopKillsTheOwnedProcessAndCanBeRepeated()
    {
        await receiver.StartAsync(new("normal",directory));
        Assert.Equal(ReceiverState.Listening, receiver.State);
        Assert.NotNull(receiver.ProcessId);
        using var child=Process.GetProcessById(receiver.ProcessId!.Value);
        await receiver.StopAsync(); await receiver.StopAsync();
        Assert.True(child.HasExited); Assert.Equal(ReceiverState.Stopped,receiver.State);
    }
    [Fact] public async Task DoubleStartIsRejected()
    {
        await receiver.StartAsync(new("normal",directory));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>receiver.StartAsync(new("normal",directory)));
    }
    [Fact] public async Task CanceledStartupLeavesNoProcess()
    {
        using var cancel=new CancellationTokenSource(200);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>receiver.StartAsync(new("stall",directory),cancel.Token));
        Assert.Null(receiver.ProcessId);
    }
    [Fact] public async Task EarlyCrashIsNotReportedAsListening()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(()=>receiver.StartAsync(new("crash",directory)));
        Assert.Equal(ReceiverState.Faulted,receiver.State); Assert.Null(receiver.ProcessId);
    }
    [Fact] public async Task OutputFloodRemainsBoundedAndRedacted()
    {
        await receiver.StartAsync(new("flood",directory)); await Task.Delay(300);
        Assert.NotEmpty(receiver.Diagnostics); Assert.True(receiver.Diagnostics.Count<=100);
        Assert.DoesNotContain(receiver.Diagnostics,x=>x.Contains("secret"));
        await receiver.StopAsync();
    }
    [Fact] public async Task RestartIsUnaffectedByOldOutput()
    {
        await receiver.StartAsync(new("stream",directory)); await Task.Delay(100); await receiver.StopAsync();
        await receiver.StartAsync(new("normal",directory)); await Task.Delay(100);
        Assert.Equal(ReceiverState.Listening,receiver.State);
    }
    [Fact] public async Task LaterCrashTransitionsToFaulted()
    {
        await receiver.StartAsync(new("crash-later",directory));
        var deadline=DateTime.UtcNow.AddSeconds(3);
        while(receiver.State!=ReceiverState.Faulted && DateTime.UtcNow<deadline) await Task.Delay(20);
        Assert.Equal(ReceiverState.Faulted,receiver.State);
    }
    [Fact] public async Task BufferedOutputCannotResurrectAnExitedReceiver()
    {
        var faulted=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.StateChanged+=(_,_)=> { if(receiver.State==ReceiverState.Listening) Thread.Sleep(5); if(receiver.State==ReceiverState.Faulted) faulted.TrySetResult(); };
        await receiver.StartAsync(new("race",directory));
        await faulted.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await Task.Delay(150);
        Assert.Equal(ReceiverState.Faulted,receiver.State);
    }
    public async Task DisposeAsync()
    {
        await receiver.DisposeAsync();
        // Windows can hold executable mappings briefly after the process handle signals exit.
        for(var attempt=0;;attempt++)
        {
            try { Directory.Delete(directory,true); break; }
            catch(UnauthorizedAccessException) when(attempt<20) { await Task.Delay(50); }
            catch(IOException) when(attempt<20) { await Task.Delay(50); }
        }
    }
}
