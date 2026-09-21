using System;
using System.IO;

namespace MyBrowserAgent.Configuration
{
    public sealed class AgentConfig
    {
        public int Port { get; set; } = 5050;
        public string ApiKey { get; set; }
        public string ChromeDriverDirectory { get; set; } = "driver";
        public string ChromeProfileDirectory { get; set; } = "ChromeProfile";
        public string ChromeBinary { get; set; }
        public bool Headless { get; set; }
        public bool AutoStartBrowser { get; set; } = true;
        public int PageLoadTimeoutSeconds { get; set; } = 60;
        public int ImplicitWaitSeconds { get; set; } = 1;

        public void Normalize(string baseDirectory)
        {
            if (Port < 1 || Port > 65535)
                throw new InvalidOperationException("Port must be between 1 and 65535.");
            if (PageLoadTimeoutSeconds < 1) PageLoadTimeoutSeconds = 60;
            if (ImplicitWaitSeconds < 0) ImplicitWaitSeconds = 0;

            ChromeDriverDirectory = ResolvePath(baseDirectory, ChromeDriverDirectory);
            ChromeProfileDirectory = ResolvePath(baseDirectory, ChromeProfileDirectory);
            if (!string.IsNullOrWhiteSpace(ChromeBinary))
                ChromeBinary = ResolvePath(baseDirectory, ChromeBinary);
        }

        private static string ResolvePath(string baseDirectory, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return baseDirectory;
            return Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(baseDirectory, path));
        }
    }
}
