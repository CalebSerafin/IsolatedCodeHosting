using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WasmConsoleApp;
internal class ExclusiveSynchronizationContext : SynchronizationContext {
    private bool done;
    public Exception? InnerException { get; set; }
    readonly AutoResetEvent workItemsWaiting = new AutoResetEvent(false);
    readonly Queue<Tuple<SendOrPostCallback, object>> items =
        new Queue<Tuple<SendOrPostCallback, object>>();

    public override void Send(SendOrPostCallback d, object state) {
        throw new NotSupportedException("We cannot send to our same thread");
    }

    public override void Post(SendOrPostCallback d, object state) {
        lock (items) {
            items.Enqueue(Tuple.Create(d, state));
        }
        workItemsWaiting.Set();
    }

    public void EndMessageLoop() {
        Post(_ => done = true, null);
    }

    public void BeginMessageLoop() {
        while (!done) {
            Console.WriteLine("ExclusiveSynchronizationContext scanning");
            Tuple<SendOrPostCallback, object> task = null;
            lock (items) {
                if (items.Count > 0) {
                    task = items.Dequeue();
                }
            }
            if (task != null) {
                task.Item1(task.Item2);
                if (InnerException != null) // the method threw an exception
                {
                    throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                }
            } else {
                Console.WriteLine("ExclusiveSynchronizationContext WaitOne");
                done = true;
                //workItemsWaiting.WaitOne();
            }
        }
    }

    public override SynchronizationContext CreateCopy() {
        return this;
    }
}

internal class ExclusiveTaskScheduler : TaskScheduler {
    private readonly ExclusiveSynchronizationContext syncContext;

    public ExclusiveTaskScheduler(ExclusiveSynchronizationContext syncContext) {
        this.syncContext = syncContext;
    }

    protected override void QueueTask(Task task) {
        syncContext.Post(_ => TryExecuteTask(task), null);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
        return false; // ToDo try pop one item or something.
    }

    protected override IEnumerable<Task> GetScheduledTasks() {
        return Enumerable.Empty<Task>();
    }

    public override int MaximumConcurrencyLevel => 1;
}

internal class ExclusiveDelay {
    public ExclusiveDelay() {

    }

    public void TriggerAfter(Action callback, TimeSpan delay) {
        TriggerOn(callback, DateTime.Now + delay);
    }

    public void TriggerOn(Action callback, DateTime notBefore) {
        if (notBefore <= DateTime.Now) {
            callback();
            return;
        }
        lock (@lock) {
            callbacks.Add((callback, notBefore));
        }
    }

    public void RefreshDelayCallbacks() {
        lock (@lock) {
            for (int i = callbacks.Count - 1; i >= 0; i--) {
                (Action callback, DateTime notBefore) = callbacks[i];
                if (notBefore > DateTime.Now)
                    continue;
                callbacks.RemoveAt(i);
                callback();
            }
        }
    }

    public static ExclusiveDelay Default = new();
    public static Task Delay(TimeSpan delay) => Default.Delay(delay);

    readonly List<(Action callback, DateTime notBefore)> callbacks = new();
    readonly object @lock = new();
}

static class ExclusiveDelay_Extensions {
    public static Task Delay(this ExclusiveDelay exclusiveDelay, TimeSpan delay) {
        var tcs = new TaskCompletionSource<bool>();
        exclusiveDelay.TriggerAfter(() => tcs.SetResult(true), delay);
        return tcs.Task;
    }
}