using DotNetIsolator;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using WasmConsoleApp;

using Wasmtime;

namespace HostForWasmConsole;

internal class Program {
    static async Task Main(string[] args) {
        // Set up the runtime
        WasiConfiguration wasiConfiguration = new WasiConfiguration()
            .WithInheritedStandardOutput()
            .WithPreopenedDirectory(Path.Combine(Environment.CurrentDirectory,"\\DummyDir"), "/")
            ;

        using var host = new IsolatedRuntimeHost()
            .WithAssemblyLoader(LoadAssembly);
        using var isolatedRuntime = new IsolatedRuntime(host);


        Console.WriteLine("Creating Isolated Object");
        var isoApp = new AsyncIsolateWorker(isolatedRuntime);

        //await Task.Delay(TimeSpan.FromSeconds(1));
        isoApp.InvokeVoid("SetTaskCompletionSource");
        await Task.Delay(TimeSpan.FromSeconds(2));

        isoApp.Dispose();

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    static string JoinArray<T>(T[] array) => string.Join(", ", array);

    static byte[]? LoadAssembly(string assemblyName) {
        string directoryPath = Path.GetDirectoryName(typeof(IsolatedRuntimeHost)!.Assembly.Location)!;
        string path = Path.Combine(directoryPath, assemblyName + ".dll");
        return (!File.Exists(path)) ? null : File.ReadAllBytes(path);
    }


    sealed class AsyncIsolateWorker : IDisposable {
        public AsyncIsolateWorker(IsolatedRuntime isolatedRuntime, CancellationToken cancellationToken = default) {
            this.isolatedRuntime = isolatedRuntime;
            isolatedObject = isolatedRuntime.CreateObject<WasmConsoleApp.Program>();

            isolatedObject.InvokeVoid("set_HostVersion", 1);
            isolatedObject.InvokeVoid("set_HostVersionFriendly", "Version 1 Test Preview");
            isolatedObject.InvokeVoid("Startup");

            workerTask = StartRuntimeAsyncWorker(isolatedObject, cancellationToken);
        }

        public void InvokeVoid(string methodName) {
            lock (@lock) {
                isolatedObject.InvokeVoid(methodName);
            }
        }

        #region Fields
        readonly IsolatedRuntime isolatedRuntime;
        readonly object @lock = new();
        readonly Task workerTask;
        readonly IsolatedObject isolatedObject;
        readonly CancellationToken cancellationToken;
        bool isDisposed;
        long issuedCallbackIds = 0;
        #endregion

        #region Private Methods
        Task StartRuntimeAsyncWorker(IsolatedObject isolatedObject, CancellationToken cancellationToken) {
            return Task.Factory.StartNew(
                (object? stateObj) => {
                    AsyncIsolateWorker state = stateObj as AsyncIsolateWorker ?? throw new ArgumentNullException(nameof(state));
                    Console.WriteLine($"Hello from StartRuntimeAsyncWorker.");
                    while (!isDisposed && !state.cancellationToken.IsCancellationRequested) {
                        lock (@lock) {
                            isolatedObject.InvokeVoid("EnumerateAsyncStateMachine");
                        }
                        Task.Yield();
                    }
                    lock (@lock) {
                        isolatedObject.InvokeVoid("End");
                    }
                },
                state: this,
                cancellationToken,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler | TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default
            );
        }

        long GetNewCallbackId() {
            return Interlocked.Increment(ref issuedCallbackIds);
        }

        #endregion
        public void Dispose() {
            isDisposed = true;
        }
    }
}

