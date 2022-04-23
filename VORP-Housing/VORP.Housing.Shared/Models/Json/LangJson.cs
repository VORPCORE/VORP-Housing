using Newtonsoft.Json;

namespace VORP.Housing.Shared.Models.Json
{
    public class LangJson
    {
        [JsonProperty("PressToBuy")]
        public string PressToBuy { get; set; }

        [JsonProperty("PressToBuyRoom")]
        public string PressToBuyRoom { get; set; }

        [JsonProperty("NoMoney")]
        public string NoMoney { get; set; }

        [JsonProperty("YouBoughtHouse")]
        public string YouBoughtHouse { get; set; }

        [JsonProperty("PressToOpen")]
        public string PressToOpen { get; set; }

        [JsonProperty("PressToClose")]
        public string PressToClose { get; set; }

        [JsonProperty("PressToEnter")]
        public string PressToEnter { get; set; }

        [JsonProperty("PressToLeave")]
        public string PressToLeave { get; set; }

        [JsonProperty("OpenInventory")]
        public string OpenInventory { get; set; }

        [JsonProperty("ErrorQuantity")]
        public string ErrorQuantity { get; set; }

        [JsonProperty("WeaponsNotAllowed")]
        public string WeaponsNotAllowed { get; set; }

        [JsonProperty("ItemInBlacklist")]
        public string ItemInBlacklist { get; set; }

        [JsonProperty("MaxWeightQuantity")]
        public string MaxWeightQuantity { get; set; }
    }
}
