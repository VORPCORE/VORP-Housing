using CitizenFX.Core;
using Newtonsoft.Json;
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
            TriggerEvent("vorp:addNewCallBack", "getHouses", new Action<int, CallbackDelegate, dynamic>(async (source, cb, anything) =>
            {
                dynamic result = await Exports["ghmattimysql"].executeSync("SELECT * FROM housing", new string[] { });
                if (result.Count != 0)
                {
                    foreach (var r in result)
                    {
                        int houseId = result.id;
                        string furniture = "{}";
                        if (!String.IsNullOrEmpty(result[0].furniture))
                        {
                            furniture = result[0].furniture;
                        }
                        Houses[houseId].Identifier = result[0].identifier;
                        Houses[houseId].Furniture = furniture;
                        Houses[houseId].IsOpen = Convert.ToBoolean(result[0].open);
                    }
                    string houses = JsonConvert.SerializeObject(Houses);
                    cb(houses);
                }
                else
                {
                    string houses = JsonConvert.SerializeObject(Houses);
                    cb(houses);
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
