namespace MyBrowserAgent.Models
{
    public sealed class OpenRequest { public string Url { get; set; } }

    public class ElementRequest
    {
        public string Selector { get; set; }
        public string SelectorType { get; set; } = "css";
    }

    public sealed class FillRequest : ElementRequest { public string Value { get; set; } }
    public sealed class AttributeRequest : ElementRequest { public string Attribute { get; set; } }
    public sealed class JavaScriptRequest { public string Script { get; set; } }
    public sealed class WindowRequest { public string Handle { get; set; } }
    public sealed class NewTabRequest { public string Url { get; set; } }

    public sealed class WaitRequest : ElementRequest
    {
        public int TimeoutSeconds { get; set; } = 30;
    }
}
