using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VORP.Housing.Client.Scripts;
using VORP.Housing.Shared;
using VORP.Housing.Shared.Models;
using VORP.Housing.Shared.Models.Json;

namespace VORP.Housing.Client
{
    public class Init : Manager
    {
        private int _charId = 0;
        private bool _isInRoom = false;
        private readonly ConfigurationSingleton _configurationInstance = ConfigurationSingleton.Instance;

        public static Dictionary<uint, House> HousesDb = new Dictionary<uint, House>();
        public static Dictionary<int, Room> RoomsDb = new Dictionary<int, Room>();

        public void Initialize()
        {
            AddEvent("vorp_housing:UpdateHousesStatus", new Action<uint, string, int>(UpdateHouse));
            AddEvent("vorp_housing:UpdateRoomsStatus", new Action<int, string, int>(UpdateRoom));
            AddEvent("vorp_housing:SetDoorState", new Action<uint, bool>(SetDoorState));
            AddEvent("vorp_housing:ListHouses", new Action<string>(ListHouses));
            AddEvent("vorp_housing:ListRooms", new Action<string>(ListRooms));
            AddEvent("vorp:SelectedCharacter", new Action<int>((charId) =>
            {
                TriggerServerEvent("vorp_housing:getHouses", charId);
                TriggerServerEvent("vorp_housing:getRooms", charId);

                _charId = charId;
            }));

            AttachTickHandler(UseInteriorCompsTickAsync);
            AttachTickHandler(ChangeStatusTickAsync);
            AttachTickHandler(DoorLocksTickAsync);
        }

        #region Private Methods

        #region Event Methods
        private void ListHouses(string json)
        {
            HousesDb = JsonConvert.DeserializeObject<Dictionary<uint, House>>(json);
            SetBlips();
        }

        private void ListRooms(string json)
        {
            RoomsDb = JsonConvert.DeserializeObject<Dictionary<int, Room>>(json);
        }

        private void SetDoorState(uint houseId, bool state)
        {
            HousesDb[houseId].IsOpen = state;
        }

        private void UpdateHouse(uint houseId, string identifier, int charIdentifier)
        {
            HousesDb[houseId].Identifier = identifier;
            HousesDb[houseId].CharIdentifier = charIdentifier;
        }

        private void UpdateRoom(int roomId, string identifier, int charIdentifier)
        {
            RoomsDb[roomId].Identifier = identifier;
            RoomsDb[roomId].CharIdentifier = charIdentifier;
        }
        #endregion

        #region Tick Methods
        private async Task UseInteriorCompsTickAsync()
        {
            try
            {
                if (_configurationInstance.Config == null || API.IsEntityDead(API.PlayerPedId()))
                {
                    return;
                }

                Vector3 playerCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);

                int interiorEntityId = API.GetInteriorFromEntity(API.PlayerPedId());
                if (HousesDb.TryGetValue((uint)interiorEntityId, out House house) && !string.IsNullOrEmpty(house.Identifier))
                {
                    HouseJson houseJson = _configurationInstance.Config.Houses.FirstOrDefault(x => x.Id == interiorEntityId);
                    float invX = (float)houseJson.Inventory[0];
                    float invY = (float)houseJson.Inventory[1];
                    float invZ = (float)houseJson.Inventory[2];
                    float invR = (float)houseJson.Inventory[3];

                    Vector3 invCoords = new Vector3(invX, invY, invZ);
                    if (Vector3.Distance(playerCoords, invCoords) <= invR)
                    {
                        Functions.DrawTxt(_configurationInstance.Language.OpenInventory, 0.5f, 0.9f, 0.7f, 0.7f, 255, 255, 255, 255, true, true);
                        if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                        {
                            TriggerEvent("vorp_inventory:OpenHouseInventory", houseJson.Name, interiorEntityId);
                            TriggerServerEvent("vorp_housing:UpdateInventoryHouse", interiorEntityId);
                        }
                    }
                }

                for (int i = 0; i < _configurationInstance.Config.Rooms.Count; i++)
                {
                    RoomJson roomJson = _configurationInstance.Config.Rooms[i];
                    int roomId = roomJson.Id;

                    float invX = (float)roomJson.Inventory[0];
                    float invY = (float)roomJson.Inventory[1];
                    float invZ = (float)roomJson.Inventory[2];
                    float invR = (float)roomJson.Inventory[3];

                    if (RoomsDb.TryGetValue(roomId, out Room room) && !string.IsNullOrEmpty(room.Identifier))
                    {
                        Vector3 invCoords = new Vector3(invX, invY, invZ);
                        if (Vector3.Distance(playerCoords, invCoords) < invR)
                        {
                            Functions.DrawTxt(_configurationInstance.Language.OpenInventory, 0.5f, 0.9f, 0.7f, 0.7f, 255, 255, 255, 255, true, true);
                            if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                            {
                                TriggerEvent("vorp_inventory:OpenHouseInventory", roomJson.Name, roomId);
                                TriggerServerEvent("vorp_housing:UpdateInventoryHouse", roomId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Client.Init.UseInteriorCompsTickAsync()");
            }
        }

        private async Task ChangeStatusTickAsync()
        {
            try
            {
                if (_configurationInstance.Config == null || API.IsEntityDead(API.PlayerPedId()))
                {
                    return;
                }

                Vector3 playerCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);

                // Check status of houses
                for (int i = 0; i < _configurationInstance.Config.Houses.Count; i++)
                {
                    HouseJson houseJson = _configurationInstance.Config.Houses[i];
                    int houseId = (int)houseJson.Id;

                    float doorStatusX = (float)houseJson.DoorsStatus[0];
                    float doorStatusY = (float)houseJson.DoorsStatus[1];
                    float doorStatusZ = (float)houseJson.DoorsStatus[2];

                    if (HousesDb.TryGetValue((uint)houseId, out House house))
                    {
                        Vector3 doorCoords = new Vector3(doorStatusX, doorStatusY, doorStatusZ);

                        if (Vector3.Distance(playerCoords, doorCoords) < 2.5f)
                        {
                            // Check if house has been bought
                            if (string.IsNullOrEmpty(house.Identifier))
                            {
                                Functions.DrawTxt3D(doorCoords, string.Format(_configurationInstance.Language.PressToBuy, houseJson.Price));
                                if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                                {
                                    TriggerServerEvent("vorp_housing:BuyHouse", houseId, houseJson.Price);
                                    await Delay(5000);
                                }
                            }

                            if (house.CharIdentifier == _charId)
                            {
                                if (house.IsOpen)
                                {
                                    await ChangeDoorStateAsync(doorCoords, house, _configurationInstance.Language.PressToClose, false);
                                }
                                else
                                {
                                    await ChangeDoorStateAsync(doorCoords, house, _configurationInstance.Language.PressToOpen, true);
                                }
                            }
                        }
                    }
                }

                // Check status of rooms
                for (int i = 0; i < _configurationInstance.Config.Rooms.Count; i++)
                {
                    RoomJson roomJson = _configurationInstance.Config.Rooms[i];
                    int roomId = roomJson.Id;

                    float doorStatusX = (float)roomJson.DoorsStatus[0];
                    float doorStatusY = (float)roomJson.DoorsStatus[1];
                    float doorStatusZ = (float)roomJson.DoorsStatus[2];

                    if (RoomsDb.TryGetValue(roomId, out Room room))
                    {
                        Vector3 doorCoords = new Vector3(doorStatusX, doorStatusY, doorStatusZ);

                        if (Vector3.Distance(playerCoords, doorCoords) < 2.5f)
                        {
                            // Check if room has been bought
                            if (string.IsNullOrEmpty(room.Identifier))
                            {
                                Functions.DrawTxt3D(doorCoords, string.Format(_configurationInstance.Language.PressToBuyRoom, roomJson.Price));
                                if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                                {
                                    TriggerServerEvent("vorp_housing:BuyRoom", roomId, roomJson.Price);
                                    await Delay(5000);
                                }
                            }
                            else
                            {
                                if (room.CharIdentifier == _charId)
                                {
                                    if (!_isInRoom)
                                    {
                                        Functions.DrawTxt3D(doorCoords, _configurationInstance.Language.PressToEnter);
                                        if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                                        {
                                            _isInRoom = true;
                                            float tpEnterX = (float)roomJson.TPEnter[0];
                                            float tpEnterY = (float)roomJson.TPEnter[1];
                                            float tpEnterZ = (float)roomJson.TPEnter[2];

                                            await TeleportPlayerWithScreenFadeAsync(tpEnterX, tpEnterY, tpEnterZ, true);
                                        }
                                    }
                                    else
                                    {
                                        Functions.DrawTxt3D(doorCoords, _configurationInstance.Language.PressToLeave);
                                        if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                                        {
                                            _isInRoom = false;
                                            float tpLeaveX = (float)roomJson.TPLeave[0];
                                            float tpLeaveY = (float)roomJson.TPLeave[1];
                                            float tpLeaveZ = (float)roomJson.TPLeave[2];

                                            await TeleportPlayerWithScreenFadeAsync(tpLeaveX, tpLeaveY, tpLeaveZ, false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Client.Init.ChangeStatusTickAsync()");
            }
        }

        private async Task DoorLocksTickAsync()
        {
            try
            {
                if (_configurationInstance.Config == null || API.IsEntityDead(API.PlayerPedId()))
                {
                    return;
                }

                Vector3 playerCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);

                // Check if house doors are locked
                for (int i = 0; i < _configurationInstance.Config.Houses.Count; i++)
                {
                    HouseJson houseJson = _configurationInstance.Config.Houses[i];
                    for (int o = 0; o < houseJson.Doors.Count; o++)
                    {
                        float doorX = (float)houseJson.Doors[o][0];
                        float doorY = (float)houseJson.Doors[o][1];
                        float doorZ = (float)houseJson.Doors[o][2];
                        float doorH = (float)houseJson.Doors[o][3]; // door heading/yaw

                        Vector3 doorCoords = new Vector3(doorX, doorY, doorZ);

                        if (Vector3.Distance(playerCoords, doorCoords) < 6.0f)
                        {
                            uint doorHash = houseJson.DoorHashes[o];
                            
                            if (HousesDb.TryGetValue(houseJson.Id, out House house) && house.IsOpen)
                            {
                                // Lock the door if it isn't already
                                if (API.DoorSystemGetDoorState(doorHash) != 0)
                                {
                                    Functions.AddDoorToSystemNew(doorHash);
                                    Functions.DoorSystemSetDoorState(doorHash, 0);
                                }
                            }
                            else
                            {
                                UnlockDoor(doorHash, doorH);
                            }
                        }
                    }
                }

                // Check if room doors are locked
                for (int i = 0; i < _configurationInstance.Config.Rooms.Count; i++)
                {
                    RoomJson roomJson = _configurationInstance.Config.Rooms[i];
                    for (int o = 0; o < roomJson.Doors.Count; o++)
                    {
                        float doorX = (float)roomJson.Doors[o][0];
                        float doorY = (float)roomJson.Doors[o][1];
                        float doorZ = (float)roomJson.Doors[o][2];
                        float doorH = (float)roomJson.Doors[o][3]; // door heading/yaw

                        Vector3 doorCoords = new Vector3(doorX, doorY, doorZ);

                        if (Vector3.Distance(playerCoords, doorCoords) < 12.0f)
                        {
                            UnlockDoor(roomJson.DoorHashes[o], doorH);
                        }
                    }
                }

                await Delay(500);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Client.Init.DoorLocksTickAsync()");
            }
        }
        #endregion

        #region Class Methods
        private void SetBlips()
        {
            try
            {
                //249721687
                foreach (HouseJson house in _configurationInstance.Config.Houses)
                {
                    if (HousesDb.ContainsKey(house.Id))
                    {
                        int blip = Function.Call<int>((Hash)0x554D9D53F696D002, 1664425300, house.DoorsStatus[0], house.DoorsStatus[1], house.DoorsStatus[2]); // Blip MAP::BLIP_ADD_FOR_COORDS
                        if (string.IsNullOrEmpty(HousesDb[house.Id].Identifier))
                        {
                            Function.Call((Hash)0x74F74D3207ED525C, blip, 249721687, 1); // void MAP::SET_BLIP_SPRITE
                        }
                        else
                        {
                            Function.Call((Hash)0x74F74D3207ED525C, blip, -2024635066, 1); // void MAP::SET_BLIP_SPRITE
                        }

                        Function.Call((Hash)0x9CB1A1623062F402, blip, house.Name); // void MAP::_SET_BLIP_NAME_FROM_PLAYER_STRING
                        Function.Call((Hash)0x174D0AAB11CED739, (int)house.Id, house.InteriorName); // void INTERIOR::ACTIVATE_INTERIOR_ENTITY_SET
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Client.Init.SetBlips()");
            }
        }

        /// <summary>
        /// Change the state of a house's door and display a prompt
        /// </summary>
        /// <param name="doorCoords">Coordinates of the house's door</param>
        /// <param name="house">The house that owns the door</param>
        /// <param name="languagePrompt">Prompt to either close or open a door</param>
        /// <param name="state">Assign the state of the door (open = true/close = false)</param>
        private async Task ChangeDoorStateAsync(Vector3 doorCoords, House house, string languagePrompt, bool state)
        {
            try
            {
                Functions.DrawTxt3D(doorCoords, languagePrompt);

                if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                {
                    house.IsOpen = state;
                    TriggerServerEvent("vorp_housing:changeDoorState", house.Id, state);
                    await Delay(3000);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Client.Init.ChangeDoorStateAsync()");
            }
        }

        /// <summary>
        /// Teleport player to a certain place with a UI screen-fade
        /// </summary>
        /// <param name="tpX">Teleport X-axis</param>
        /// <param name="tpY">Teleport Y-axis</param>
        /// <param name="tpZ">Teleport Z-axis</param>
        /// <param name="shouldSetInstancePlayer"></param>
        /// <returns></returns>
        private async Task TeleportPlayerWithScreenFadeAsync(float tpX, float tpY, float tpZ, bool shouldSetInstancePlayer)
        {
            try
            {
                API.DoScreenFadeOut(500);
                await Delay(600);
                API.SetEntityCoords(API.PlayerPedId(), tpX, tpY, tpZ, false, false, false, false);
                await Delay(100);
                TriggerEvent("vorp:setInstancePlayer", shouldSetInstancePlayer);
                API.DoScreenFadeIn(500);
                await Delay(3000);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Client.Init.TeleportPlayerWithScreenFadeAsync()");
            }
        }

        /// <summary>
        /// Unlock door for a room/house via "door hash" and provide heading/yaw of door
        /// </summary>
        /// <param name="doorHash">Door's hash</param>
        /// <param name="doorH">Door's heading/yaw for door positioning</param>
        private void UnlockDoor(uint doorHash, float doorH)
        {
            try
            {
                int entity = Functions.GetEntityByDoorHash(doorHash);

                if (API.DoorSystemGetOpenRatio(doorHash) != 0.0f)
                {
                    Functions.DoorSystemSetOpenRatio(doorHash);
                    API.SetEntityRotation(entity, 0.0f, 0.0f, doorH, 2, true);
                }

                // Unlock the door if it isn't already
                if (API.DoorSystemGetDoorState(doorHash) != 1)
                {
                    Functions.AddDoorToSystemNew(doorHash);
                    Functions.DoorSystemSetDoorState(doorHash, 1);
                    API.SetEntityRotation(entity, 0.0f, 0.0f, doorH, 2, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Client.Init.UnlockDoor()");
            }
        }
        #endregion

        #endregion
    }
}
