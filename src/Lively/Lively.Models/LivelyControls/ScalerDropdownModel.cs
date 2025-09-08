using Newtonsoft.Json;

namespace Lively.Models.LivelyControls
{
    public class ScalerDropdownModel : ControlModel, IDropdownItem
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("items")]
        public string[] Items { get; set; }

        public ScalerDropdownModel() : base("scalerDropdown") { }
    }
}
