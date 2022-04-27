using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using VORP.Housing.Server.Extensions;
using VORP.Housing.Server.Scripts;
using VORP.Housing.Shared;
using VORP.Housing.Shared.Models;
using VORP.Housing.Shared.Models.Json;

namespace VORP.Housing.Server
{
    public class Init : Manager
    {
        private readonly ConfigurationSingleton _configurationInstance = ConfigurationSingleton.Instance;

        public static Dictionary<uint, House> Houses = new Dictionary<uint, House>();
        public static Dictionary<int, Room> Rooms = new Dictionary<int, Room>();
        
        public void Initialize()
        {
            AddEvent("vorp_housing:BuyHouse", new Action<Player, uint, double>(BuyHouseAsync));
            AddEvent("vorp_housing:BuyRoom", new Action<Player, int, double>(BuyRoomAsync));
            AddEvent("vorp_housing:changeDoorState", new Action<uint, bool>(ChangeDoorState));
            AddEvent("vorp_housing:getRooms", new Action<int>(GetRoomsAsync));
            AddEvent("vorp_housing:getHouses", new Action<int>(GetHousesAsync));
        }

        #region Public Method
        public void LoadAll()
        {
            foreach (HouseJson house in _configurationInstance.Config.Houses)
            {
                Houses.Add(house.Id, new House
                {
                    Id = house.Id,
                    Interior = house.InteriorName, 
                    Identifier = null, 
                    CharIdentifier = -1, 
                    Price = house.Price, 
                    Furniture = null, 
                    IsOpen = false, 
                    MaxWeight = house.MaxWeight
                });
            }

            foreach (RoomJson room in _configurationInstance.Config.Rooms)
            {
                Rooms.Add(room.Id, new Room
                {
                    Id = room.Id, 
                    Identifier = null, 
                    CharIdentifier = -1, 
                    Price = room.Price, 
                    MaxWeight = room.MaxWeight
                });
            }
        }
        #endregion

        #region Private Methods
        private async void GetRoomsAsync(int source)
        {
            try
            {
                Player player = PlayerList[source];
                if (player == null)
                {
                    Logger.Error($"Server.Init.GetRoomsAsync(): Player \"{source}\" does not exist.");
                    return;
                }

                dynamic tableExist = await Export["ghmattimysql"].executeSync("SELECT * FROM information_schema.tables WHERE table_schema = 'vorpv2' AND table_name = 'rooms' LIMIT 1;", new string[] { });
                if (tableExist.Count == 0)
                {
                    Logger.Error("Server.Init.GetRoomsAsync(): SQL table \"rooms\" doesn't exist");
                    return;
                }

                Dictionary<int, Room> rooms = Rooms.ToDictionary(h => h.Key, h => h.Value);
                string sid = "steam:" + player.Identifiers["steam"];

                int charIdentifier = await player.GetCoreUserCharacterIdAsync();
                if (charIdentifier == -1)
                {
                    Logger.Warn("Server.Init.GetRoomsAsync(): Player charIdentifier returned -1. " +
                        "Skipping charIdentifier-based SQL lookup for rooms...");
                }
                else
                {
                    dynamic result = await Export["ghmattimysql"].executeSync("SELECT * FROM rooms WHERE identifier = ? AND charidentifier = ?", new object[] { sid, charIdentifier });

                    if (result != null && result.Count > 0)
                    {
                        Logger.Debug(JsonConvert.SerializeObject(result));

                        foreach (var r in result)
                        {
                            int roomId = r.interiorId;
                            string identifier = r.identifier;
                            int charidentifier = r.charidentifier;
                            rooms[roomId].Identifier = identifier;
                            rooms[roomId].CharIdentifier = charidentifier;
                        }
                    }
                }

                string roomsString = JsonConvert.SerializeObject(rooms);
                TriggerClientEvent("vorp_housing:ListRooms", roomsString);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.GetRoomsAsync()");
            }
        }

        private async void GetHousesAsync(int source)
        {
            try
            {
                Player player = PlayerList[source];
                if (player == null)
                {
                    Logger.Error($"Server.Init.GetHousesAsync(): Player \"{source}\" does not exist.");
                    return;
                }

                string sid = "steam:" + player.Identifiers["steam"];

                Dictionary<uint, House> houses = Houses.ToDictionary(h => h.Key, h => h.Value);

                dynamic tableExist = await Export["ghmattimysql"].executeSync("SELECT * FROM information_schema.tables WHERE table_schema = 'vorpv2' AND table_name = 'housing' LIMIT 1;", new string[] { });
                if (tableExist.Count == 0)
                {
                    Logger.Error("Server.Init.GetHousesAsync(): SQL table \"housing\" doesn't exist");
                    return;
                }

                dynamic result = await Export["ghmattimysql"].executeSync("SELECT * FROM housing", new string[] { });

                if (result != null && result.Count > 0)
                {
                    Logger.Debug(JsonConvert.SerializeObject(result));

                    foreach (var r in result)
                    {
                        uint houseId = ConvertValue(r.id.ToString());
                        string identifier = r.identifier;
                        int charidentifier = r.charidentifier;
                        string furniture = "{}";
                        if (!string.IsNullOrEmpty(r.furniture))
                        {
                            furniture = r.furniture;
                        }

                        houses[houseId].Identifier = identifier;
                        houses[houseId].CharIdentifier = charidentifier;
                        houses[houseId].Furniture = furniture;
                        houses[houseId].IsOpen = Convert.ToBoolean(r.open);

                        if (identifier.Equals(sid))
                        {
                            houses[houseId].IsOwner = true;
                        }
                    }
                }

                string housesString = JsonConvert.SerializeObject(houses);
                TriggerClientEvent("vorp_housing:ListHouses", housesString);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.GetHousesAsync()");
            }
        }

        private void ChangeDoorState(uint houseId, bool state)
        {
            Houses[houseId].SetOpen(state);
            TriggerClientEvent("vorp_housing:SetDoorState", houseId, state);
        }

        private async void BuyHouseAsync([FromSource]Player player, uint houseId, double price)
        {
            string sid = "steam:" + player.Identifiers["steam"];
            int source = int.Parse(player.Handle);
            dynamic userCharacter = await player.GetCoreUserCharacterAsync();
            int charIdentifier = userCharacter.charIdentifier;
            double money = userCharacter.money;
            if (money >= price)
            {
                TriggerEvent("vorp:removeMoney", source, 0, price);
                Houses[houseId].BuyHouse(sid, charIdentifier);
                TriggerClientEvent("vorp_housing:UpdateHousesStatus", houseId, sid);
                player.TriggerEvent("vorp_housing:SetHouseOwner", houseId);
                player.TriggerEvent("vorp:TipRight", _configurationInstance.Language.YouBoughtHouse, 4000);
            }
            else
            {
                player.TriggerEvent("vorp:TipRight", _configurationInstance.Language.NoMoney, 4000);
            }
        }

        private async void BuyRoomAsync([FromSource] Player player, int roomId, double price)
        {
            string sid = "steam:" + player.Identifiers["steam"];
            int source = int.Parse(player.Handle);
            dynamic userCharacter = await player.GetCoreUserCharacterAsync();
            int charIdentifier = userCharacter.charIdentifier;
            double money = userCharacter.money;
            if (money >= price)
            {
                TriggerEvent("vorp:removeMoney", source, 0, price);
                Export["ghmattimysql"].execute($"INSERT INTO rooms (interiorId, identifier, charidentifier) VALUES (?, ?, ?)", new object[] { roomId, sid, charIdentifier });
                player.TriggerEvent("vorp_housing:UpdateRoomsStatus", roomId, sid);
                //player.TriggerEvent("vorp_housing:SetHouseOwner", roomId);
                player.TriggerEvent("vorp:TipRight", _configurationInstance.Language.YouBoughtHouse, 4000);
            }
            else
            {
                player.TriggerEvent("vorp:TipRight", _configurationInstance.Language.NoMoney, 4000);
            }
        }

        private static uint ConvertValue(string s)
        {
            if (uint.TryParse(s, out uint result))
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
        #endregion
    }
}
