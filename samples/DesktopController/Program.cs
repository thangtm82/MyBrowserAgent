using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DesktopController
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                RunAsync(args).GetAwaiter().GetResult();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static async Task RunAsync(string[] args)
        {
            if (args.Length == 0) { PrintUsage(); return; }

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vps.json");
            if (!File.Exists(configPath))
                throw new FileNotFoundException("vps.json not found. Copy vps.sample.json to vps.json.", configPath);

            var configs = JsonConvert.DeserializeObject<List<VpsConfig>>(File.ReadAllText(configPath)) ?? new List<VpsConfig>();
            if (configs.Count == 0) throw new InvalidOperationException("No VPS entries found in vps.json.");

            var clients = configs.Select(x => new BrowserAgentClient(x.Name, x.BaseUrl, x.ApiKey)).ToList();
            try
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "status":
                        var statuses = await Task.WhenAll(clients.Select(async x => new { Client = x, Status = await x.GetStatusAsync() }));
                        foreach (var item in statuses)
                            Console.WriteLine("{0,-10} running={1,-5} url={2}", item.Client.Name, item.Status?.Running, item.Status?.Url ?? "-");
                        break;

                    case "start":
                        await Task.WhenAll(clients.Select(x => x.StartAsync()));
                        Console.WriteLine("Started {0} agent(s).", clients.Count);
                        break;

                    case "stop":
                        await Task.WhenAll(clients.Select(x => x.StopAsync()));
                        Console.WriteLine("Stopped {0} browser(s).", clients.Count);
                        break;

                    case "open":
                        if (args.Length < 2) throw new ArgumentException("Usage: DesktopController.exe open <url>");
                        await Task.WhenAll(clients.Select(x => x.OpenAsync(args[1])));
                        Console.WriteLine("Opened URL on {0} VPS(s).", clients.Count);
                        break;

                    case "screenshot":
                        if (args.Length < 2) throw new ArgumentException("Usage: DesktopController.exe screenshot <output-directory>");
                        Directory.CreateDirectory(args[1]);
                        await Task.WhenAll(clients.Select(x => x.ScreenshotAsync(Path.Combine(args[1], x.Name + ".png"))));
                        Console.WriteLine("Saved {0} screenshot(s).", clients.Count);
                        break;

                    default:
                        PrintUsage();
                        break;
                }
            }
            finally
            {
                foreach (var client in clients) client.Dispose();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("DesktopController commands:");
            Console.WriteLine("  status");
            Console.WriteLine("  start");
            Console.WriteLine("  stop");
            Console.WriteLine("  open <url>");
            Console.WriteLine("  screenshot <output-directory>");
        }
    }
}
