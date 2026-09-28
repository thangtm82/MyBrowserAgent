using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyBrowserAgent.Models
{
    public sealed class TargetBidUpdateItem
    {
        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        [JsonProperty("countryCodes")]
        public IList<string> CountryCodes { get; set; }

        [JsonProperty("bid")]
        public string Bid { get; set; }
    }
}
