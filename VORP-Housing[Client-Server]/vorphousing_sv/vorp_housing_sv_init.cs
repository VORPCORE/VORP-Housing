using CitizenFX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vorphousing_sv
{
    public class vorp_housing_sv_init : BaseScript
    {
        public static Dictionary<uint, House> Houses = new Dictionary<uint, House>();
        public static Dictionary<int, Room> Rooms = new Dictionary<int, Room>();

        public static dynamic VORPCORE;
        
        public vorp_housing_sv_init()
        {
            EventHandlers["vorp_housing:BuyHouse"] += new Action<Player, uint, double>(BuyHouse);
            EventHandlers["vorp_housing:BuyRoom"] += new Action<Player, int, double>(BuyRoom);
            EventHandlers["vorp_housing:changeDoorState"] += new Action<uint, bool>(ChangeDoorState);
            EventHandlers["vorp_housing:getRooms"] += new Action<int, CallbackDelegate>(GetRooms);
            EventHandlers["vorp_housing:getHouses"] += new Action<int, CallbackDelegate>(GetHouses);

            TriggerEvent("getCore", new Action<dynamic>((dic) =>
            {
                VORPCORE = dic;
            }));
        }

        public async void GetRooms(int source, CallbackDelegate cb)
        {
            try
            {
                dynamic UserCharacter = VORPCORE.getUser(source).getUsedCharacter;
                int charIdentifier = UserCharacter.charIdentifier;

                PlayerList PL = Players;
                Player _source = PL[source];
                string sid = "steam:" + _source.Identifiers["steam"];

                dynamic result = await Exports["ghmattimysql"].executeSync("SELECT * FROM rooms WHERE identifier = ? AND charidentifier = ?", new object[] { sid, charIdentifier });

                Dictionary<int, Room> _Rooms = Rooms.ToDictionary(h => h.Key, h => h.Value);

                if (result.Count != 0)
                {
                    foreach (var r in result)
                    {
                        int roomId = r.interiorId;
                        string identifier = r.identifier;
                        int charidentifier = r.charidentifier;
                        _Rooms[roomId].Identifier = identifier;
                        _Rooms[roomId].CharIdentifier = charidentifier;

                    }
                    string rooms = JsonConvert.SerializeObject(_Rooms);
                    cb(rooms);
                }
                else
                {
                    string rooms = JsonConvert.SerializeObject(_Rooms);
                    cb(rooms);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async void GetHouses(int source, CallbackDelegate cb)
        {
            try
            {
                dynamic result = await Exports["ghmattimysql"].executeSync("SELECT * FROM housing", new string[] { });

                PlayerList PL = Players;
                Player _source = PL[source];
                string sid = "steam:" + _source.Identifiers["steam"];

                Dictionary<uint, House> _Houses = Houses.ToDictionary(h => h.Key, h => h.Value);

                if (result.Count != 0)
                {
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
