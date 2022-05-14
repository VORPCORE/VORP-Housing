using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public static Dictionary<uint, House> HousesDb = new Dictionary<uint, House>();
        public static Dictionary<int, Room> RoomsDb = new Dictionary<int, Room>();
        
        public void Initialize()
        {
            AddEvent("vorp_housing:BuyHouse", new Action<Player, uint, double>(OnBuyingHouseAsync));
            AddEvent("vorp_housing:BuyRoom", new Action<Player, int, double>(OnBuyingRoomAsync));
            AddEvent("vorp_housing:changeDoorState", new Action<uint, bool>(OnChangingDoorState));
            AddEvent("vorp_housing:getRooms", new Action<int>(OnGettingRoomsAsync));
            AddEvent("vorp_housing:getHouses", new Action<int>(OnGettingHousesAsync));
        }

        #region Public Method
        public void LoadAll()
        {
            try
            {
                foreach (HouseJson house in _configurationInstance.Config.Houses)
                {
                    HousesDb.Add(house.Id, new House
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
                    RoomsDb.Add(room.Id, new Room
                    {
                        Id = room.Id,
                        Identifier = null,
                        CharIdentifier = -1,
                        Price = room.Price,
                        MaxWeight = room.MaxWeight
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.LoadAll()");
            }
        }
        #endregion

        #region Private Methods

        #region Event Methods
        private async void OnGettingRoomsAsync(int source)
        {
            try
            {
                Player player = PlayerList[source];
                if (player == null)
                {
                    Logger.Error($"Server.Init.OnGettingRoomsAsync(): Player \"{source}\" does not exist.");

                    TriggerClientListRooms(); // return default list
                    return;
                }

                if (!await HasSqlTable("rooms"))
                {
                    Logger.Error("Server.Init.OnGettingRoomsAsync(): SQL table \"rooms\" doesn't exist. " +
                        "Users will not be able to buy rooms unless this table exists");

                    TriggerClientListRooms(); // return default list
                    return;
                }

                dynamic result = await Export["ghmattimysql"].executeSync("SELECT * FROM rooms", new string[] { });

                if (result != null && result.Count > 0)
                {
                    Logger.Debug(JsonConvert.SerializeObject(result));

                    foreach (var r in result)
                    {
                        int roomId = r.interiorId;
                        string identifier = r.identifier;
                        int charidentifier = r.charidentifier;
                        RoomsDb[roomId].Identifier = identifier;
                        RoomsDb[roomId].CharIdentifier = charidentifier;
                    }
                }

                TriggerClientListRooms();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.OnGettingRoomsAsync()");
            }
        }

        private async void OnGettingHousesAsync(int source)
        {
            try
            {
                Player player = PlayerList[source];
                if (player == null)
                {
                    Logger.Error($"Server.Init.OnGettingHousesAsync(): Player \"{source}\" does not exist.");
                    TriggerClientListHouses(); // return default list
                    return;
                }

                if (!await HasSqlTable("housing"))
                {
                    Logger.Error("Server.Init.OnGettingHousesAsync(): SQL table \"housing\" doesn't exist. " +
                        "Users will not be able to buy houses unless this table exists");

                    TriggerClientListHouses(); // return default list
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

                        HousesDb[houseId].Identifier = identifier;
                        HousesDb[houseId].CharIdentifier = charidentifier;
                        HousesDb[houseId].Furniture = furniture;
                        HousesDb[houseId].IsOpen = Convert.ToBoolean(r.open);
                    }
                }

                TriggerClientListHouses();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.OnGettingHousesAsync()");
            }
        }

        private void OnChangingDoorState(uint houseId, bool state)
        {
            try
            {
                HousesDb[houseId].IsOpen = state;
                int openState = state ? 1 : 0;

                Export["ghmattimysql"].execute($"UPDATE housing SET open=? WHERE id=?", new object[] { openState, houseId });

                TriggerClientEvent("vorp_housing:SetDoorState", houseId, state);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.OnChangingDoorState()");
            }
        }

        private async void OnBuyingHouseAsync([FromSource]Player player, uint houseId, double price)
        {
            try
            {
                string sid = "steam:" + player.Identifiers["steam"];
                int source = int.Parse(player.Handle);
                dynamic userCharacter = await player.GetCoreUserCharacterAsync();
                int charIdentifier = userCharacter.charIdentifier;

                double money = userCharacter.money;
                if (money >= price)
                {
                    TriggerEvent("vorp:removeMoney", source, 0, price);

                    HousesDb[houseId].Identifier = sid;
                    HousesDb[houseId].CharIdentifier = charIdentifier;

                    Export["ghmattimysql"].execute($"INSERT INTO housing (id, identifier, charidentifier, furniture) VALUES (?, ?, ?, ?)", new object[] { houseId, sid, charIdentifier, "{}" });
                    TriggerClientEvent("vorp_housing:UpdateHousesStatus", houseId, sid, charIdentifier);
                    player.TriggerEvent("vorp:TipRight", _configurationInstance.Language.YouBoughtHouse, 4000);
                }
                else
                {
                    player.TriggerEvent("vorp:TipRight", _configurationInstance.Language.NoMoney, 4000);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.OnBuyingHouseAsync()");
            }
        }

        private async void OnBuyingRoomAsync([FromSource] Player player, int roomId, double price)
        {
            try
            {
                string sid = "steam:" + player.Identifiers["steam"];
                int source = int.Parse(player.Handle);
                dynamic userCharacter = await player.GetCoreUserCharacterAsync();
                int charIdentifier = userCharacter.charIdentifier;

                double money = userCharacter.money;
                if (money >= price)
                {
                    TriggerEvent("vorp:removeMoney", source, 0, price);

                    RoomsDb[roomId].Identifier = sid;
                    RoomsDb[roomId].CharIdentifier = charIdentifier;

                    Export["ghmattimysql"].execute($"INSERT INTO rooms (interiorId, identifier, charidentifier) VALUES (?, ?, ?)", new object[] { roomId, sid, charIdentifier });
                    player.TriggerEvent("vorp_housing:UpdateRoomsStatus", roomId, sid, charIdentifier);
                    player.TriggerEvent("vorp:TipRight", _configurationInstance.Language.YouBoughtHouse, 4000);
                }
                else
                {
                    player.TriggerEvent("vorp:TipRight", _configurationInstance.Language.NoMoney, 4000);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.OnBuyingRoomAsync()");
            }
        }
        #endregion

        #region Class Methods
        private async Task<bool> HasSqlTable(string tableName)
        {
            // Note: DATABASE() returns the default (current) database name as a string in the utf8 character set.
            // Reference: https://mariadb.com/kb/en/database/
            dynamic tableExist = await Export["ghmattimysql"].executeSync(
                    "SELECT * FROM information_schema.tables " +
                    $"WHERE table_schema = DATABASE() AND table_name = '{tableName}' " +
                    "LIMIT 1;",
                    new string[] { });

            if (tableExist?.Count > 0)
            {
                return true;
            }

            return false;
        }

        private void TriggerClientListRooms()
        {
            try
            {
                string roomsString = JsonConvert.SerializeObject(RoomsDb);
                TriggerClientEvent("vorp_housing:ListRooms", roomsString);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.TriggerClientListRooms()");
            }
        }

        private void TriggerClientListHouses()
        {
            try
            {
                string housesString = JsonConvert.SerializeObject(HousesDb);
                TriggerClientEvent("vorp_housing:ListHouses", housesString);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Server.Init.TriggerClientListHouses()");
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

        #endregion
    }
}
