# IsolatedCodeHosting
Built on top of [@SteveSandersonMS's DotNetIsolator](https://github.com/SteveSandersonMS/DotNetIsolator) <br/>
Provides additional features such as async await. <br/>
And a temporary custom version of Delay(Timespan delay). <br/>

## Why weird and custom systems?
Threads are not available. Therefore, the Task-based Asynchronous Pattern is implemented with a shared async state machine. <br/>
The state machine is enumerated by having the Wasm host call into the Wasm app. <br/>
If the host does not repeatably call into the app, tasks will not be processed. <br/>
The temporary replacement for `Task.Delay` works similarly to this. <br/>

## Next Steps
Find a suitable replacement or fix for Timers. <br/>
Fixing Timers will allow the regular `Task.Delay` method to work and other .NET features. <br/>
