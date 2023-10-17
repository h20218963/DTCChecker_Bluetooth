using Newtonsoft.Json;

namespace DTCChecker.Items
{
    public class DTCval
    {
        [JsonProperty("DTCCodes")]
        public string DTCCodes { get; set; }
        [JsonProperty("DTCDetails")]
        public string DTCDetails { get; set; }
    }
}
