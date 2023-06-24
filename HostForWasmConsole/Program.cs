using DotNetIsolator;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

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

        isolatedRuntime.RegisterCallback("DelayedCallback", (string message) => {
            Console.WriteLine($"DelayedCallback: {message}");
        });

        Console.WriteLine("Creating Isolated Object");
        var isoApp = isolatedRuntime.CreateObject<WasmConsoleApp.Program>();
        isoApp.InvokeVoid("set_HostVersion", 1);
        isoApp.InvokeVoid("set_HostVersionFriendly", "Version 1 Test Preview");

        Console.WriteLine("Calling Startup");
        isoApp.InvokeVoid("Startup");

        await Task.Delay(TimeSpan.FromSeconds(3));

        Console.WriteLine("Calling End");
        isoApp.InvokeVoid("End");
    }


    static long GetCryptoRandomLong() {
        return (((long)RandomNumberGenerator.GetInt32(int.MaxValue)) << 32) + RandomNumberGenerator.GetInt32(int.MaxValue);
    }

    static string JoinArray<T>(T[] array) => string.Join(", ", array);

    static byte[]? LoadAssembly(string assemblyName) {
        string directoryPath = Path.GetDirectoryName(typeof(IsolatedRuntimeHost)!.Assembly.Location)!;
        string path = Path.Combine(directoryPath, assemblyName + ".dll");
        return (!File.Exists(path)) ? null : File.ReadAllBytes(path);
    }
}

