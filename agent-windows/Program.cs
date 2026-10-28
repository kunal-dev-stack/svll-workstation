using System;
using System.Threading;
using System.Threading.Tasks;

namespace AegisMeshAgent
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            string serverUrl = "ws://localhost:5000";
            string token = "MESH-ENTERPRISE-TOKEN-2026";
            string? deviceId = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--server" && i + 1 < args.Length) serverUrl = args[++i];
                else if (args[i] == "--token" && i + 1 < args.Length) token = args[++i];
                else if (args[i] == "--deviceId" && i + 1 < args.Length) deviceId = args[++i];
                else if (args[i] == "--help")
                {
                    Console.WriteLine("AegisMesh Endpoint Agent for Windows");
                    Console.WriteLine("Usage: AegisMeshAgent [options]");
                    Console.WriteLine("  --server <url>     Mesh Gateway WebSocket URL (default: ws://localhost:5000)");
                    Console.WriteLine("  --token <token>    Enrollment security token");
                    Console.WriteLine("  --deviceId <id>    Custom device identifier");
                    return;
                }
            }

            Console.WriteLine("==================================================================");
            Console.WriteLine("🛡️  AegisMesh Endpoint Management Agent - Windows");
            Console.WriteLine("    Zero-Evasion Standard OS Architecture | Fully AV Compatible");
            Console.WriteLine($"    Gateway: {serverUrl}");
            Console.WriteLine($"    Enrollment Token: {token}");
            Console.WriteLine("==================================================================");

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n[AGENT] Shutting down cleanly...");
                cts.Cancel();
            };

            var agent = new AgentCore(serverUrl, token, deviceId);
            await agent.StartAsync(cts.Token);
        }
    }
}
