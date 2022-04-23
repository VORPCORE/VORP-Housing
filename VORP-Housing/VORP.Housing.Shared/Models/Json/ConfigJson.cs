using Newtonsoft.Json;
using System.Collections.Generic;

namespace VORP.Housing.Shared.Models.Json
{
    public class RoomJson
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Price")]
        public double Price { get; set; }

        [JsonProperty("DoorsStatus")]
        public List<double> DoorsStatus { get; set; }

        [JsonProperty("Doors")]
        public List<List<double>> Doors { get; set; }

        [JsonProperty("Inventory")]
        public List<double> Inventory { get; set; }

        [JsonProperty("TPEnter")]
        public List<double> TPEnter { get; set; }

        [JsonProperty("TPLeave")]
        public List<double> TPLeave { get; set; }

        [JsonProperty("MaxWeight")]
        public int MaxWeight { get; set; }
    }

    public class HouseJson
    {
        [JsonProperty("Id")]
        public uint Id { get; set; }

        [JsonProperty("InteriorName")]
        public string InteriorName { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Price")]
        public double Price { get; set; }

        [JsonProperty("DoorsStatus")]
        public List<double> DoorsStatus { get; set; }

        [JsonProperty("Doors")]
        public List<List<double>> Doors { get; set; }

        [JsonProperty("Inventory")]
        public List<double> Inventory { get; set; }

        [JsonProperty("MaxWeight")]
        public int MaxWeight { get; set; }
    }

    public class ConfigJson
    {
        [JsonProperty("defaultlang")]
        public string DefaultLang { get; set; }

        [JsonProperty("ItemsBlacklist")]
        public List<string> ItemsBlacklist { get; set; }

        [JsonProperty("Rooms")]
        public List<RoomJson> Rooms { get; set; }

        [JsonProperty("Houses")]
        public List<HouseJson> Houses { get; set; }
    }
}
