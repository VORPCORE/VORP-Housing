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
            EventHandlers["vorp_housing:BuyHouse"] += new Action<Player, int, double>(BuyHouse);
            EventHandlers["vorp_housing:changeDoorState"] += new Action<int, bool>(ChangeDoorState);

            TriggerEvent("vorp:addNewCallBack", "getHouses", new Action<int, CallbackDelegate, dynamic>(async (source, cb, anything) =>
            {
                try
                {
                    dynamic result = await Exports["ghmattimysql"].executeSync("SELECT * FROM housing", new string[] { });

                    PlayerList PL = new PlayerList();
                    Player _source = PL[source];
                    string sid = "steam:" + _source.Identifiers["steam"];

                    Dictionary<int, House> _Houses = Houses.ToDictionary(h => h.Key, h => h.Value);

                    if (result.Count != 0)
                    {
                        foreach (var r in result)
                        {
                            int houseId = r.id;
                            string identifier = r.identifier;
                            string furniture = "{}";
                            if (!String.IsNullOrEmpty(r.furniture))
                            {
                                furniture = r.furniture;
                            }
                            _Houses[houseId].Identifier = identifier;
                            _Houses[houseId].Furniture = furniture;
                            _Houses[houseId].IsOpen = Convert.ToBoolean(r.open);

                            if (identifier.Equals(sid))
                            {
                                _Houses[houseId].IsOwner = true;
                            }

                        }
                        string houses = JsonConvert.SerializeObject(_Houses);
                        cb(houses);
                    }
                    else
                    {
                        string houses = JsonConvert.SerializeObject(_Houses);
                        cb(houses);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
               
            }));
        }

        public void ChangeDoorState(int houseId, bool state)
        {
            Houses[houseId].SetOpen(state);
            TriggerClientEvent("vorp_housing:SetDoorState", houseId, state);
        }

        public void BuyHouse([FromSource]Player source, int houseId, double price)
        {
            string sid = "steam:" + source.Identifiers["steam"];
            int _source = int.Parse(source.Handle);
            TriggerEvent("vorp:getCharacter", _source, new Action<dynamic>((user) =>
            {
                double money = user.money;
                if (money >= price)
                {
                    TriggerEvent("vorp:removeMoney", _source, 0, price);
                    Houses[houseId].BuyHouse(sid);
                    TriggerClientEvent("vorp_housing:UpdateHousesStatus", houseId, sid);
                    source.TriggerEvent("vorp_housing:SetHouseOwner", houseId);
                    source.TriggerEvent("vorp:TipRight", LoadConfig.Langs["YouBoughtHouse"], 4000);
                }
                else
                {
                    source.TriggerEvent("vorp:TipRight", LoadConfig.Langs["NoMoney"], 4000);
                }
            }));
        }

        public static async Task LoadHouses()
        {
            foreach (var house in LoadConfig.Config["Houses"])
            {
                Houses.Add(house["Id"].ToObject<int>(), new House(house["Id"].ToObject<int>(), house["InteriorName"].ToString(), null, house["Price"].ToObject<double>(), null, false, house["MaxWeight"].ToObject<int>()));
            }

        }

    }
}
