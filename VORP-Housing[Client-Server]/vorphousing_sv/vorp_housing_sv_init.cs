using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vorphousing_sv
{
    public class vorp_housing_sv_init : BaseScript
    {
        public static Dictionary<int, House> Houses = new Dictionary<int, House>();

        public vorp_housing_sv_init()
        {
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnecting);
        }

        private void OnPlayerConnecting([FromSource]Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {
            string sid = "steam:" + player.Identifiers["steam"];
            Exports["ghmattimysql"].execute("SELECT * FROM housing WHERE identifier LIKE ?", new string[] { sid.ToString() }, new Action<dynamic>((result) =>
            {
                if (result.Count != 0)
                {
                    int houseId = result.id;
                    string furniture = "{}";
                    if (!String.IsNullOrEmpty(result[0].inventory))
                    {
                        furniture = result[0].furniture;
                    }
                    Houses[houseId].Identifier = sid;
                    Houses[houseId].Furtniture = furniture;
                    Houses[houseId].IsOpen = Convert.ToBoolean(result[0].open);
                }
            }));
        }

        public static async Task LoadHouses()
        {
            foreach (var house in LoadConfig.Config["Houses"])
            {
                Houses.Add(house["Id"].ToObject<int>(), new House(house["Id"].ToObject<int>(), house["InteriorName"].ToString(), null, house["Price"].ToObject<double>(), null, false));
            }
        }

    }
}
