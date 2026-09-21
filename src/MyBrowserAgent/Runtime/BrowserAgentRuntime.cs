using MyBrowserAgent.Configuration;
using MyBrowserAgent.Services;

namespace MyBrowserAgent.Runtime
{
    public static class BrowserAgentRuntime
    {
        public static AgentConfig Config { get; set; }
        public static BrowserService Browser { get; set; }
    }
}
