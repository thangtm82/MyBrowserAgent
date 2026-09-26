using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Text.RegularExpressions;
using MyBrowserAgent.Configuration;
using MyBrowserAgent.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace MyBrowserAgent.Services
{
    public sealed class BrowserService : IDisposable
    {
        private readonly AgentConfig _config;
        private readonly object _sync = new object();
        private ChromeDriver _driver;
        private static readonly Regex ProfileArgument = new Regex(
            @"(?:^|\s)(?:""--user-data-dir=(?<whole>[^""]+)""|--user-data-dir=(?:""(?<quoted>[^""]+)""|(?<plain>[^\s""]+)))",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public BrowserService(AgentConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsRunning
        {
            get { lock (_sync) return _driver != null; }
        }

        public void Start()
        {
            lock (_sync)
            {
                if (_driver != null)
                {
                    try
                    {
                        var handle = _driver.CurrentWindowHandle;
                        return;
                    }
                    catch (WebDriverException)
                    {
                        Stop();
                    }
                }

                Directory.CreateDirectory(_config.ChromeDriverDirectory);
                Directory.CreateDirectory(_config.ChromeProfileDirectory);

                var driverExe = Path.Combine(_config.ChromeDriverDirectory, "chromedriver.exe");
                if (!File.Exists(driverExe))
                    throw new FileNotFoundException(
                        "chromedriver.exe was not found. Put the ChromeDriver matching Chrome 109 in the configured driver directory.",
                        driverExe);

                if (!string.IsNullOrWhiteSpace(_config.ChromeBinary) && !File.Exists(_config.ChromeBinary))
                    throw new FileNotFoundException("Chrome executable was not found.", _config.ChromeBinary);

                var options = new ChromeOptions();
                if (!string.IsNullOrWhiteSpace(_config.ChromeBinary))
                    options.BinaryLocation = _config.ChromeBinary;

                options.AddArgument("--user-data-dir=" + _config.ChromeProfileDirectory);
                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-popup-blocking");
                options.AddArgument("--no-first-run");
                options.AddArgument("--no-default-browser-check");
                options.AddArgument("--start-maximized");

                if (_config.Headless)
                {
                    options.AddArgument("--headless");
                    options.AddArgument("--window-size=1920,1080");
                }

                try
                {
                    _driver = CreateDriver(options);
                }
                catch (WebDriverException startupError) when (IsProfileStartupFailure(startupError))
                {
                    int closed;
                    try
                    {
                        closed = CloseChromeUsingProfile(_config.ChromeProfileDirectory);
                    }
                    catch (Exception cleanupError)
                    {
                        throw new InvalidOperationException(
                            "Chrome could not start and the Agent could not close the Chrome process using its profile.",
                            new AggregateException(startupError, cleanupError));
                    }

                    if (closed == 0) throw;
                    Console.WriteLine("Closed " + closed + " Chrome process(es) using the Agent profile. Retrying startup.");
                    Thread.Sleep(500);
                    _driver = CreateDriver(options);
                }
            }
        }

        private ChromeDriver CreateDriver(ChromeOptions options)
        {
            var service = ChromeDriverService.CreateDefaultService(_config.ChromeDriverDirectory);
            service.HideCommandPromptWindow = true;
            ChromeDriver driver = null;
            try
            {
                driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(120));
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(_config.PageLoadTimeoutSeconds);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(_config.ImplicitWaitSeconds);
                return driver;
            }
            catch
            {
                try { driver?.Quit(); } catch { }
                try { driver?.Dispose(); } catch { }
                try { service.Dispose(); } catch { }
                throw;
            }
        }

        private static bool IsProfileStartupFailure(WebDriverException error)
        {
            var message = error.ToString();
            return message.IndexOf("DevToolsActivePort", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("user data directory is already in use", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CloseChromeUsingProfile(string profileDirectory)
        {
            var profile = Path.GetFullPath(profileDirectory).TrimEnd('\\', '/');
            var matchingPids = new List<int>();

            using (var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'chrome.exe'"))
            using (var processes = searcher.Get())
            {
                foreach (ManagementObject process in processes)
                {
                    using (process)
                    {
                        var commandLine = process["CommandLine"] as string;
                        if (UsesProfile(commandLine, profile))
                            matchingPids.Add(Convert.ToInt32(process["ProcessId"]));
                    }
                }
            }

            var closed = 0;
            foreach (var pid in matchingPids)
            {
                Process process;
                try { process = Process.GetProcessById(pid); }
                catch (ArgumentException) { continue; }

                using (process)
                {
                    if (process.HasExited) continue;
                    if (process.CloseMainWindow())
                        process.WaitForExit(3000);
                    if (!process.HasExited)
                    {
                        process.Kill();
                        if (!process.WaitForExit(5000))
                            throw new InvalidOperationException("Chrome using the Agent profile did not exit.");
                    }
                    closed++;
                }
            }

            return closed;
        }

        private static bool UsesProfile(string commandLine, string profile)
        {
            if (string.IsNullOrEmpty(commandLine)) return false;
            foreach (Match match in ProfileArgument.Matches(commandLine))
            {
                var value = match.Groups["whole"].Success ? match.Groups["whole"].Value :
                    match.Groups["quoted"].Success ? match.Groups["quoted"].Value :
                    match.Groups["plain"].Value;
                try
                {
                    var path = Path.GetFullPath(value).TrimEnd('\\', '/');
                    if (string.Equals(path, profile, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch (ArgumentException) { }
                catch (NotSupportedException) { }
            }
            return false;
        }

        public object GetStatus()
        {
            lock (_sync)
            {
                if (_driver == null)
                    return new { running = false, url = (string)null, title = (string)null };

                try
                {
                    return new { running = true, url = _driver.Url, title = _driver.Title };
                }
                catch (WebDriverException ex)
                {
                    return new { running = false, url = (string)null, title = (string)null, error = ex.Message };
                }
            }
        }

        public void Open(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Url is required.", nameof(url));
            lock (_sync) Driver.Navigate().GoToUrl(url);
        }

        public void Click(string selector, string selectorType)
        {
            lock (_sync) FindElement(selector, selectorType).Click();
        }

        public void Fill(string selector, string value, string selectorType)
        {
            lock (_sync)
            {
                var element = FindElement(selector, selectorType);
                element.Clear();
                element.SendKeys(value ?? string.Empty);
            }
        }

        public void SendKeys(string selector, string value, string selectorType)
        {
            lock (_sync) FindElement(selector, selectorType).SendKeys(value ?? string.Empty);
        }

        public string GetText(string selector, string selectorType)
        {
            lock (_sync) return FindElement(selector, selectorType).Text;
        }

        public string GetAttribute(string selector, string attribute, string selectorType)
        {
            if (string.IsNullOrWhiteSpace(attribute)) throw new ArgumentException("Attribute is required.", nameof(attribute));
            lock (_sync) return FindElement(selector, selectorType).GetAttribute(attribute);
        }

        public string ReadPageSourceInTemporaryTab(string url, Func<string, bool> ready, int timeoutSeconds)
        {
            if (ready == null) throw new ArgumentNullException(nameof(ready));
            if (timeoutSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

            return RunInTemporaryTab(url, driver =>
            {
                var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
                string source;
                do
                {
                    source = driver.PageSource;
                    if (ready(source)) return source;
                    if (DateTime.UtcNow >= deadline) break;
                    Thread.Sleep(250);
                } while (true);
                return source;
            });
        }

        public T RunInTemporaryTab<T>(string url, Func<IWebDriver, T> action)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Url is required.", nameof(url));
            if (action == null) throw new ArgumentNullException(nameof(action));

            lock (_sync)
            {
                var driver = Driver;
                var original = driver.CurrentWindowHandle;
                string temporary = null;
                try
                {
                    var before = driver.WindowHandles.ToList();
                    ((IJavaScriptExecutor)driver).ExecuteScript("window.open('about:blank','_blank');");
                    temporary = driver.WindowHandles.Except(before).Single();
                    driver.SwitchTo().Window(temporary);
                    driver.Navigate().GoToUrl(url);
                    return action(driver);
                }
                finally
                {
                    if (temporary != null && driver.WindowHandles.Contains(temporary))
                    {
                        driver.SwitchTo().Window(temporary);
                        driver.Close();
                    }
                    if (driver.WindowHandles.Contains(original))
                        driver.SwitchTo().Window(original);
                }
            }
        }

        public string GetHtml()
        {
            lock (_sync) return Driver.PageSource;
        }

        public object ExecuteJavaScript(string script)
        {
            if (string.IsNullOrWhiteSpace(script)) throw new ArgumentException("Script is required.", nameof(script));
            lock (_sync) return ((IJavaScriptExecutor)Driver).ExecuteScript(script);
        }

        public byte[] Screenshot()
        {
            lock (_sync) return ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;
        }

        public IList<BrowserCookieDto> GetCookies(bool includeValues)
        {
            lock (_sync)
            {
                return Driver.Manage().Cookies.AllCookies
                    .Select(x => new BrowserCookieDto
                    {
                        Name = x.Name,
                        Value = includeValues ? x.Value : null,
                        Domain = x.Domain,
                        Path = x.Path,
                        Expiry = x.Expiry,
                        Secure = x.Secure,
                        IsHttpOnly = x.IsHttpOnly
                    }).ToList();
            }
        }

        public void DeleteAllCookies()
        {
            lock (_sync) Driver.Manage().Cookies.DeleteAllCookies();
        }

        public IList<WindowDto> GetWindows()
        {
            lock (_sync)
            {
                var driver = Driver;
                var original = driver.CurrentWindowHandle;
                var result = new List<WindowDto>();

                try
                {
                    foreach (var handle in driver.WindowHandles)
                    {
                        driver.SwitchTo().Window(handle);
                        result.Add(new WindowDto { Handle = handle, Url = driver.Url, Title = driver.Title });
                    }
                }
                finally
                {
                    if (driver.WindowHandles.Contains(original))
                        driver.SwitchTo().Window(original);
                }

                return result;
            }
        }

        public void SwitchWindow(string handle)
        {
            if (string.IsNullOrWhiteSpace(handle)) throw new ArgumentException("Handle is required.", nameof(handle));
            lock (_sync) Driver.SwitchTo().Window(handle);
        }

        public void NewTab(string url)
        {
            lock (_sync)
            {
                var driver = Driver;
                ((IJavaScriptExecutor)driver).ExecuteScript("window.open('about:blank','_blank');");
                var handle = driver.WindowHandles.Last();
                driver.SwitchTo().Window(handle);
                if (!string.IsNullOrWhiteSpace(url))
                    driver.Navigate().GoToUrl(url);
            }
        }

        public void CloseCurrentTab()
        {
            lock (_sync)
            {
                var driver = Driver;
                if (driver.WindowHandles.Count <= 1)
                    throw new InvalidOperationException("Refusing to close the last browser tab.");

                driver.Close();
                driver.SwitchTo().Window(driver.WindowHandles.Last());
            }
        }

        public void Refresh() { lock (_sync) Driver.Navigate().Refresh(); }
        public void Back() { lock (_sync) Driver.Navigate().Back(); }
        public void Forward() { lock (_sync) Driver.Navigate().Forward(); }

        public bool WaitForElement(string selector, int timeoutSeconds, string selectorType)
        {
            if (timeoutSeconds <= 0) timeoutSeconds = 30;
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    lock (_sync)
                    {
                        if (FindElement(selector, selectorType) != null) return true;
                    }
                }
                catch (NoSuchElementException) { }
                catch (StaleElementReferenceException) { }

                Thread.Sleep(250);
            }

            return false;
        }

        public void Stop()
        {
            lock (_sync)
            {
                var driver = _driver;
                _driver = null;
                if (driver == null) return;
                try { driver.Quit(); } catch { }
                try { driver.Dispose(); } catch { }
            }
        }

        public void Dispose() => Stop();

        private ChromeDriver Driver
        {
            get
            {
                if (_driver == null) Start();
                return _driver;
            }
        }

        private IWebElement FindElement(string selector, string selectorType)
        {
            if (string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("Selector is required.", nameof(selector));
            var type = (selectorType ?? "css").Trim().ToLowerInvariant();

            switch (type)
            {
                case "id": return Driver.FindElement(By.Id(selector));
                case "name": return Driver.FindElement(By.Name(selector));
                case "xpath": return Driver.FindElement(By.XPath(selector));
                case "tag": return Driver.FindElement(By.TagName(selector));
                case "class": return Driver.FindElement(By.ClassName(selector));
                case "linktext": return Driver.FindElement(By.LinkText(selector));
                case "partiallinktext": return Driver.FindElement(By.PartialLinkText(selector));
                case "css": return Driver.FindElement(By.CssSelector(selector));
                default: throw new ArgumentException("Unsupported SelectorType. Use css, id, name, xpath, tag, class, linktext or partiallinktext.");
            }
        }
    }
}
