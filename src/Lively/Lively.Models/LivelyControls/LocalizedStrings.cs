using Newtonsoft.Json;

namespace Lively.Models.LivelyControls;

public class LocalizedStrings
{
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }

    [JsonProperty("help")]
    public string Help { get; set; }

    [JsonProperty("items")]
    public string[] Items { get; set; }
}
