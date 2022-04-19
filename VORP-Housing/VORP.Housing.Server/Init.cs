using CitizenFX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VORP.Housing.Shared.Diagnostics;

namespace vorphousing_sv
{
    public class Init : BaseScript
    {
        public static Dictionary<uint, House> Houses = new Dictionary<uint, House>();
        public static Dictionary<int, Room> Rooms = new Dictionary<int, Room>();

        public static dynamic VORPCORE;
        
        public Init()
        {
            EventHandlers["vorp_housing:BuyHouse"] += new Action<Player, uint, double>(BuyHouse);
            EventHandlers["vorp_housing:BuyRoom"] += new Action<Player, int, double>(BuyRoom);
            EventHandlers["vorp_housing:changeDoorState"] += new Action<uint, bool>(ChangeDoorState);
            EventHandlers["vorp_housing:getRooms"] += new Action<int>(GetRooms);
            EventHandlers["vorp_housing:getHouses"] += new Action<int>(GetHouses);
        }

        public async void GetRooms(int source)//, CallbackDelegate cb)
        {
            try
            {
                // TODO: Convert this into a method similar to Inventory's PluginManager class
                if (VORPCORE == null)
                {
                    Logger.Error("Server.Init.GetRooms(): VORPCORE is null");
                    return;
                }

                // TODO: Convert this into a method similar to Inventory's PluginManager class
                if (VORPCORE.getUser(source) == null)
                {
                    Logger.Error("Server.Init.GetRooms(): VORPCORE.getUser(source) is null");
                    return;
                }

                dynamic UserCharacter = VORPCORE.getUser(source).getUsedCharacter;
                int charIdentifier = UserCharacter.charIdentifier;

                PlayerList PL = Players;
                Player _source = PL[source];
                string sid = "steam:" + _source.Identifiers["steam"];

                dynamic tableExist = await Exports["ghmattimysql"].executeSync("SELECT * FROM information_schema.tables WHERE table_schema = 'vorpv2' AND table_name = 'rooms' LIMIT 1;", new string[] { });
                if (tableExist.Count == 0)
                {
                    Logger.Error("Server.Init.GetRooms(): SQL table \"rooms\" doesn't exist");
                    return;
                }

                dynamic result = await Exports["ghmattimysql"].executeSync("SELECT * FROM rooms WHERE identifier = ? AND charidentifier = ?", new object[] { sid, charIdentifier });
                Logger.Debug(JsonConvert.SerializeObject(result));

                Dictionary<int, Room> _Rooms = Rooms.ToDictionary(h => h.Key, h => h.Value);

                if (result != null && result.Count > 0)
                {
                    foreach (var r in result)
                    {
                        int roomId = r.interiorId;
                        string identifier = r.identifier;
                        int charidentifier = r.charidentifier;
                        _Rooms[roomId].Identifier = identifier;
                        _Rooms[roomId].CharIdentifier = charidentifier;

                    }
                }

                string rooms = JsonConvert.SerializeObject(_Rooms);
                TriggerClientEvent("vorp_housing:ListRooms", rooms);
            }
            catch (Exception ex)
            {
                Logger.Error($"Server.Init.GetRooms(): {ex.Message}");
            }
        }

        public async void GetHouses(int source)//, CallbackDelegate cb)
        {
            try
            {
                PlayerList PL = Players;
                Player _source = PL[source];
                string sid = "steam:" + _source.Identifiers["steam"];

                Dictionary<uint, House> _Houses = Houses.ToDictionary(h => h.Key, h => h.Value);

                dynamic tableExist = await Exports["ghmattimysql"].executeSync("SELECT * FROM information_schema.tables WHERE table_schema = 'vorpv2' AND table_name = 'housing' LIMIT 1;", new string[] { });
                if (tableExist.Count == 0)
                {
                    Logger.Error("Server.Init.GetHouses(): SQL table \"housing\" doesn't exist");
                    return;
                }

                dynamic result = await Exports["ghmattimysql"].executeSync("SELECT * FROM housing", new string[] { });

                if (result != null && result.Count > 0)
                {
                    Logger.Debug(JsonConvert.SerializeObject(result));

                    foreach (var r in result)
                    {
                        uint houseId = ConvertValue(r.id.ToString());
                        string identifier = r.identifier;
                        int charidentifier = r.charidentifier;
                        string furniture = "{}";
                        if (!String.IsNullOrEmpty(r.furniture))
                        {
                            furniture = r.furniture;
                        }
                        _Houses[houseId].Identifier = identifier;
                        _Houses[houseId].CharIdentifier = charidentifier;
                        _Houses[houseId].Furniture = furniture;
                        _Houses[houseId].IsOpen = Convert.ToBoolean(r.open);

                        if (identifier.Equals(sid))
                        {
                            _Houses[houseId].IsOwner = true;
                        }

                    }
                    string houses = JsonConvert.SerializeObject(_Houses);
                    TriggerClientEvent("vorp_housing:ListHouses", houses);
                    //cb(houses);
                }
                else
                {
                    string houses = JsonConvert.SerializeObject(_Houses);
                    TriggerClientEvent("vorp_housing:ListHouses", houses);
                    //cb(houses);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Server.Init.GetHouses(): {ex.Message}");
            }
        }

        public void ChangeDoorState(uint houseId, bool state)
        {
            Houses[houseId].SetOpen(state);
            TriggerClientEvent("vorp_housing:SetDoorState", houseId, state);
        }

        public void BuyHouse([FromSource]Player source, uint houseId, double price)
        {
            string sid = "steam:" + source.Identifiers["steam"];
            int _source = int.Parse(source.Handle);
            dynamic UserCharacter = VORPCORE.getUser(_source).getUsedCharacter;
            int charIdentifier = UserCharacter.charIdentifier;
            double money = UserCharacter.money;
            if (money >= price)
            {
                TriggerEvent("vorp:removeMoney", _source, 0, price);
                Houses[houseId].BuyHouse(sid, charIdentifier);
                TriggerClientEvent("vorp_housing:UpdateHousesStatus", houseId, sid);
                source.TriggerEvent("vorp_housing:SetHouseOwner", houseId);
                source.TriggerEvent("vorp:TipRight", LoadConfig.Langs["YouBoughtHouse"], 4000);
            }
            else
            {
                source.TriggerEvent("vorp:TipRight", LoadConfig.Langs["NoMoney"], 4000);
            }
        }

        public async void BuyRoom([FromSource] Player source, int roomId, double price)
        {
            string sid = "steam:" + source.Identifiers["steam"];
            int _source = int.Parse(source.Handle);
            dynamic UserCharacter = VORPCORE.getUser(_source).getUsedCharacter;
            int charIdentifier = UserCharacter.charIdentifier;
            double money = UserCharacter.money;
            if (money >= price)
            {
                TriggerEvent("vorp:removeMoney", _source, 0, price);
                Exports["ghmattimysql"].execute($"INSERT INTO rooms (interiorId, identifier, charidentifier) VALUES (?, ?, ?)", new object[] { roomId, sid, charIdentifier });
                source.TriggerEvent("vorp_housing:UpdateRoomsStatus", roomId, sid);
                //source.TriggerEvent("vorp_housing:SetHouseOwner", roomId);
                source.TriggerEvent("vorp:TipRight", LoadConfig.Langs["YouBoughtHouse"], 4000);
            }
            else
            {
                source.TriggerEvent("vorp:TipRight", LoadConfig.Langs["NoMoney"], 4000);
            }
        }

        public static async Task LoadAll()
        {
            foreach (var house in LoadConfig.Config["Houses"])
            {
                Houses.Add(house["Id"].ToObject<uint>(), new House(house["Id"].ToObject<uint>(), house["InteriorName"].ToString(), null, -1, house["Price"].ToObject<double>(), null, false, house["MaxWeight"].ToObject<int>()));
            }

            foreach (var room in LoadConfig.Config["Rooms"])
            {
                Rooms.Add(room["Id"].ToObject<int>(), new Room(room["Id"].ToObject<int>(), null, -1, room["Price"].ToObject<double>(), room["MaxWeight"].ToObject<int>()));
            }

        }

        public static uint ConvertValue(string s)
        {
            uint result;

            if (uint.TryParse(s, out result))
            {
                return result;
            }
            else
            {
                int interesante = int.Parse(s);
                result = (uint)interesante;
                return result;
            }
        }

    }
}
