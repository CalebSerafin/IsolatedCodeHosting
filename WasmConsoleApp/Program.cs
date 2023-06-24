using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using DotNetIsolator;

using Shared;

namespace WasmConsoleApp;

public class Program : IIsolatedApp {
    public static void Main() { }

    public int HostVersion { get; set; } = -1;
    public string HostVersionFriendly { get; set; } = "Un-Initialized";

    Program() {
        Console.WriteLine("Hello from the isolated app constructor");
    }

    public void Startup() {
        Console.WriteLine("Hello from the isolated app startup");
        Console.WriteLine($"Loaded on host version: {HostVersion}, {HostVersionFriendly}");
    }

    public void End() {
        Console.WriteLine("Hello from the isolated app end");
    }
}
