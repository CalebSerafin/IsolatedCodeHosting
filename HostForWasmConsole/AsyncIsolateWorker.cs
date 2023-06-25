using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetIsolator;

namespace HostForWasmConsole;
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
