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
    public static async Task Main() {
        Console.WriteLine("Hello, this app is intended to run from the HostForWasmConsole. However, this can be used as a debugging method.");


        Program isolatedWorker = new();
        _ = Task.Run(isolatedWorker.EnumerateAsyncStateMachine);
        _ = Task.Run(isolatedWorker.Startup);
        await Task.Delay(TimeSpan.FromSeconds(1));

        isolatedWorker.SetTaskCompletionSource();

        await Task.Delay(TimeSpan.FromSeconds(1));

        // Dispose early to show that worker end method is called.
        isolatedWorker.End();

        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    public int HostVersion { get; set; } = -1;
    public string HostVersionFriendly { get; set; } = "Un-Initialized";

    public Program() {
        Console.WriteLine("Hello from the isolated app constructor");
        if (isWasm) {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            taskScheduler = new ExclusiveTaskScheduler(synchronizationContext);
            taskFactory = new TaskFactory(cancellationTokenSource.Token, TaskCreationOptions.None, TaskContinuationOptions.None, taskScheduler);

            typeof(TaskScheduler)
               .GetField("s_defaultTaskScheduler", BindingFlags.Static | BindingFlags.NonPublic)!
               .SetValue(null, taskScheduler);
        } else {
            taskScheduler = TaskScheduler.Default;
            taskFactory = Task.Factory;
        }
    }

    public void Startup() {
        _ = StartupAsync();
    }

    public async Task StartupAsync() {
        Console.WriteLine("Hello from the isolated app startup async");
        Console.WriteLine($"Loaded on host version: {HostVersion}, {HostVersionFriendly}; runtime: {RuntimeInformation.OSArchitecture}");

        await Task.Run(() => {
            Console.WriteLine($"Hello nested Task.Run.");
        });

        _ = Task.Factory.StartNew(() => {
            Console.WriteLine($"Hello from Task.Factory.StartNew.");
        });

        await tcs.Task;
        Console.WriteLine("Finished awaiting tcs.Task");
        await ExclusiveDelay.Delay(TimeSpan.FromSeconds(1));
        Console.WriteLine("Finished awaiting Task.Delay");
    }

    public void SetTaskCompletionSource() {
        Console.WriteLine($"Setting TaskCompletionSource!");
        tcs.SetResult();
    }

    public void EnumerateAsyncStateMachine() {
        synchronizationContext.BeginMessageLoop();
        ExclusiveDelay.Default.RefreshDelayCallbacks();
    }

    public void End() {
        Console.WriteLine("Hello from the isolated app end");
    }

    readonly TaskCompletionSource tcs = new();
    readonly CancellationTokenSource cancellationTokenSource = new();
    readonly ExclusiveSynchronizationContext synchronizationContext = new();
    readonly TaskScheduler taskScheduler;
    readonly TaskFactory taskFactory;
    readonly bool isWasm = RuntimeInformation.OSArchitecture == Architecture.Wasm;
}
