using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using DotNetIsolator;

using Shared;

namespace WasmConsoleApp;

public class Program : IIsolatedApp {
    public static void Main() { }

    public int HostVersion { get; set; } = -1;
    public string HostVersionFriendly { get; set; } = "Un-Initialized";

    Program() {
        Console.WriteLine("Hello from the isolated app constructor");
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        taskScheduler = new(synchronizationContext);
        taskFactory = new(cancellationTokenSource.Token, TaskCreationOptions.None, TaskContinuationOptions.None, taskScheduler);

        typeof(TaskScheduler)
           .GetField("s_defaultTaskScheduler", BindingFlags.Static | BindingFlags.NonPublic)!
           .SetValue(null, taskScheduler);
    }

    public void Startup() {
        _ = StartupAsync();
        synchronizationContext.BeginMessageLoop();
    }

    public async Task StartupAsync() {
        Console.WriteLine("Hello from the isolated app startup async");
        Console.WriteLine($"Loaded on host version: {HostVersion}, {HostVersionFriendly}");
        Console.WriteLine($"Hello from StartupAsync on runtime: {RuntimeInformation.OSArchitecture}");

        await Task.Run(() => {
            Console.WriteLine($"Hello from nested Task.Run with hopefully right context.");
        });

        _ = Task.Factory.StartNew(() => {
            Console.WriteLine($"Hello from nested Task.Factory.StartNew with hopefully right context.");
        });

        await tcs.Task;
        Console.WriteLine("Finished awaiting tcs.Task");
        //await Task.Delay(TimeSpan.FromSeconds(1));
        //Timer timer = new Timer((object? state) => { Console.WriteLine("Inside Timer!"); }, null, 0, 1000);
        Console.WriteLine("Finished awaiting Task.Delay");
    }

    public void SetTaskCompletionSource() {
        Console.WriteLine($"Setting TaskCompletionSource!");
        tcs.SetResult();
        //AsyncHelpers.RunSync(async () => { Console.WriteLine($"refresh state machine"); });
    }

    public void End() {
        Console.WriteLine("Hello from the isolated app end");
    }

    readonly TaskCompletionSource tcs = new();
    readonly CancellationTokenSource cancellationTokenSource = new();
    readonly ExclusiveSynchronizationContext synchronizationContext = new();
    readonly ExclusiveTaskScheduler taskScheduler;
    readonly TaskFactory taskFactory;
}
