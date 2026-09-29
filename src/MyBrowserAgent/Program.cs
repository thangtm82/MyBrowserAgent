using System;
using System.IO;
using System.Threading;
using Microsoft.Owin.Hosting;
using MyBrowserAgent.Configuration;
using MyBrowserAgent.Runtime;
using MyBrowserAgent.Services;
using Newtonsoft.Json;

namespace MyBrowserAgent
{
    internal static class Program
    {
        private const string InstanceMutexName = @"Global\MyBrowserAgent";

        private static int Main(string[] args)
        {
            Mutex instanceMutex;
            bool createdNew;
            try
            {
                instanceMutex = new Mutex(true, InstanceMutexName, out createdNew);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine("Could not acquire the Agent instance lock: " + ex.Message);
                return 1;
            }

            using (instanceMutex)
            {
                if (!createdNew)
                {
                    Console.WriteLine("MyBrowserAgent is already running. Exiting.");
                    return 0;
                }

                try
                {
                    return RunAgent();
                }
                finally
                {
                    instanceMutex.ReleaseMutex();
                }
            }
        }

        private static int RunAgent()
        {
            Console.Title = "MyBrowserAgent";
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(baseDirectory, "config.json");

            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine("ERROR: config.json not found.");
                Console.Error.WriteLine("Copy config.sample.json to config.json and edit it.");
                return 2;
            }

            try
            {
                var config = JsonConvert.DeserializeObject<AgentConfig>(File.ReadAllText(configPath));
                if (config == null) throw new InvalidOperationException("config.json is empty or invalid.");

                var envKey = Environment.GetEnvironmentVariable("MYBROWSERAGENT_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey)) config.ApiKey = envKey;
                if (string.IsNullOrWhiteSpace(config.ApiKey))
                    throw new InvalidOperationException("ApiKey is required. Set it in config.json or MYBROWSERAGENT_API_KEY.");

                config.Normalize(baseDirectory);
                BrowserAgentRuntime.Config = config;
                BrowserAgentRuntime.Browser = new BrowserService(config);

                var listenUrl = "http://+:" + config.Port + "/";
                Console.WriteLine("MyBrowserAgent");
                Console.WriteLine("Listening: " + listenUrl);
                Console.WriteLine("Chrome profile: " + config.ChromeProfileDirectory);
                Console.WriteLine("ChromeDriver: " + config.ChromeDriverDirectory);

                using (WebApp.Start<Startup>(listenUrl))
                {
                    if (config.AutoStartBrowser)
                    {
                        Console.WriteLine("Browser: starting...");
                        try
                        {
                            BrowserAgentRuntime.Browser.Start();
                            Console.WriteLine("Browser: started");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Browser start failed: " + ex.Message);
                            Console.Error.WriteLine("The HTTP API remains available; POST /api/browser/start can retry.");
                        }
                    }

                    Console.WriteLine("READY");
                    Console.WriteLine("Press Ctrl+C or ENTER to exit.");

                    var exit = new System.Threading.ManualResetEvent(false);
                    Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Set(); };

                    var inputThread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            // Scheduled Task may have no stdin; EOF must not stop the agent.
                            if (Console.ReadLine() != null) exit.Set();
                        }
                        catch { }
                    });
                    inputThread.IsBackground = true;
                    inputThread.Start();
                    exit.WaitOne();
                }

                BrowserAgentRuntime.Browser.Dispose();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}
