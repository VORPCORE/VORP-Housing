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
        private readonly ConfigurationSingleton _configurationInstance = ConfigurationSingleton.Instance;
        private bool _isInRoom = false;

        public static Dictionary<int, House> Houses = new Dictionary<int, House>();
        public static Dictionary<int, Room> Rooms = new Dictionary<int, Room>();

        public void Initialize()
        {
            AddEvent("vorp_housing:UpdateHousesStatus", new Action<int, string>(UpdateHouse));
            AddEvent("vorp_housing:UpdateRoomsStatus", new Action<int, string>(UpdateRoom));
            AddEvent("vorp_housing:SetHouseOwner", new Action<int>(SetHouseOwner));
            AddEvent("vorp_housing:SetDoorState", new Action<int, bool>(SetDoorState));
            AddEvent("vorp_housing:ListHouses", new Action<string>(ListHouses));
            AddEvent("vorp_housing:ListRooms", new Action<string>(ListRooms));
            AddEvent("vorp:SelectedCharacter", new Action<int>((charId) =>
            {
                TriggerServerEvent("vorp_housing:getHouses", charId);
                TriggerServerEvent("vorp_housing:getRooms", charId);
            }));

            AttachTickHandler(UseInteriorCompsTickAsync);
            AttachTickHandler(ChangeStatusTickAsync);
            AttachTickHandler(DoorLockedsTickAsync);
        }

        #region Private Methods
        private void ListHouses(string json)
        {
            Logger.Trace($"Client.Init.ListHouses():\r\n{json}");
            Houses = JsonConvert.DeserializeObject<Dictionary<int, House>>(json);
            SetBlips();
        }

        private void ListRooms(string json)
        {
            Logger.Trace($"Client.Init.ListRooms():\r\n{json}");
            Rooms = JsonConvert.DeserializeObject<Dictionary<int, Room>>(json);
            //SetBlips();
            foreach (var r in Rooms)
            {
                Logger.Trace(r.Key.ToString());
            }
        }

        private void SetDoorState(int houseId, bool state)
        {
            Houses[houseId].IsOpen = state;
        }

        private void UpdateHouse(int houseId, string identifier)
        {
            Houses[houseId].Identifier = identifier;
        }

        private void UpdateRoom(int roomId, string identifier)
        {
            Rooms[roomId].Identifier = identifier;
        }

        private void SetHouseOwner(int houseId)
        {
            Houses[houseId].IsOwner = true;
        }

        private void SetBlips()
        {
            try
            {
                //249721687
                foreach (HouseJson house in _configurationInstance.Config.Houses)
                {
                    if (Houses.ContainsKey((int)house.Id))
                    {
                        int blip = Function.Call<int>((Hash)0x554D9D53F696D002, 1664425300, house.DoorsStatus[0], house.DoorsStatus[1], house.DoorsStatus[2]); // Blip MAP::BLIP_ADD_FOR_COORDS
                        if (string.IsNullOrEmpty(Houses[(int)house.Id].Identifier))
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
                Logger.Error(ex, $"Server.Init.SetBlips()");
            }
        }

        [Tick]
        private async Task UseInteriorCompsTickAsync()
        {
            if (_configurationInstance.Config == null)
            {
                return;
            }

            Vector3 pCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);
            int interiorIsIn = API.GetInteriorFromEntity(API.PlayerPedId());
            if (Houses.ContainsKey(interiorIsIn))
            {
                if (Houses[interiorIsIn].IsOwner)
                {
                    HouseJson houseIsIn = _configurationInstance.Config.Houses.FirstOrDefault(x => x.Id == interiorIsIn);
                    float invX = (float)houseIsIn.Inventory[0];
                    float invY = (float)houseIsIn.Inventory[1];
                    float invZ = (float)houseIsIn.Inventory[2];
                    float invR = (float)houseIsIn.Inventory[3];
                    
                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, invX, invY, invZ, true) <= invR)
                    {
                        Functions.DrawTxt(_configurationInstance.Language.OpenInventory, 0.5f, 0.9f, 0.7f, 0.7f, 255, 255, 255, 255, true, true);
                        if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                        {
                            TriggerEvent("vorp_inventory:OpenHouseInventory", houseIsIn.Name, interiorIsIn);
                            TriggerServerEvent("vorp_housing:UpdateInventoryHouse", interiorIsIn);
                        }
                    }
                }
            }

            for (int i = 0; i < _configurationInstance.Config.Rooms.Count; i++)
            {
                int roomId = _configurationInstance.Config.Rooms[i].Id;

                float invX = (float)_configurationInstance.Config.Rooms[i].Inventory[0];
                float invY = (float)_configurationInstance.Config.Rooms[i].Inventory[1];
                float invZ = (float)_configurationInstance.Config.Rooms[i].Inventory[2];
                float invR = (float)_configurationInstance.Config.Rooms[i].Inventory[3];

                if (Rooms.ContainsKey(roomId))
                {
                    if (!string.IsNullOrEmpty(Rooms[roomId].Identifier))
                    {
                        if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, invX, invY, invZ, true) < invR)
                        {
                            Functions.DrawTxt(_configurationInstance.Language.OpenInventory, 0.5f, 0.9f, 0.7f, 0.7f, 255, 255, 255, 255, true, true);
                            if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                            {
                                TriggerEvent("vorp_inventory:OpenHouseInventory", _configurationInstance.Config.Rooms[i].Name, roomId);
                                TriggerServerEvent("vorp_housing:UpdateInventoryHouse", roomId);
                            }
                        }
                    }
                }
            }
        }

        [Tick]
        private async Task ChangeStatusTickAsync()
        {
            if (_configurationInstance.Config == null)
            {
                return;
            }

            for (int i = 0; i < _configurationInstance.Config.Houses.Count; i++)
            {
                int houseId = (int)_configurationInstance.Config.Houses[i].Id;

                float doorStatusX = (float)_configurationInstance.Config.Houses[i].DoorsStatus[0];
                float doorStatusY = (float)_configurationInstance.Config.Houses[i].DoorsStatus[1];
                float doorStatusZ = (float)_configurationInstance.Config.Houses[i].DoorsStatus[2];

                if (Houses.ContainsKey(houseId))
                {
                    Vector3 pCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);

                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, doorStatusX, doorStatusY, doorStatusZ, true) < 2.5f)
                    {
                        if (string.IsNullOrEmpty(Houses[houseId].Identifier))
                        {
                            Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), string.Format(_configurationInstance.Language.PressToBuy, _configurationInstance.Config.Houses[i].Price));
                            if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                            {
                                TriggerServerEvent("vorp_housing:BuyHouse", houseId, _configurationInstance.Config.Houses[i].Price);
                                await Delay(5000);
                            }
                        }

                        if (Houses[houseId].IsOwner)
                        {
                            if (Houses[houseId].IsOpen)
                            {
                                Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), _configurationInstance.Language.PressToClose);

                                if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                                {
                                    Houses[houseId].IsOpen = false;
                                    TriggerServerEvent("vorp_housing:changeDoorState", houseId, false);
                                    await Delay(3000);
                                }
                            }
                            else
                            {
                                Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), _configurationInstance.Language.PressToOpen);

                                if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                                {
                                    Houses[houseId].IsOpen = true;
                                    TriggerServerEvent("vorp_housing:changeDoorState", houseId, true);
                                    await Delay(3000);
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < _configurationInstance.Config.Rooms.Count; i++)
            {
                int roomId = _configurationInstance.Config.Rooms[i].Id;

                float doorStatusX = (float)_configurationInstance.Config.Rooms[i].DoorsStatus[0];
                float doorStatusY = (float)_configurationInstance.Config.Rooms[i].DoorsStatus[1];
                float doorStatusZ = (float)_configurationInstance.Config.Rooms[i].DoorsStatus[2];

                if (Rooms.ContainsKey(roomId))
                {
                    Vector3 pCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);

                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, doorStatusX, doorStatusY, doorStatusZ, true) < 2.5f)
                    {
                        if (string.IsNullOrEmpty(Rooms[roomId].Identifier))
                        {
                            Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), string.Format(_configurationInstance.Language.PressToBuyRoom, _configurationInstance.Config.Rooms[i].Price));
                            if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                            {
                                TriggerServerEvent("vorp_housing:BuyRoom", roomId, _configurationInstance.Config.Rooms[i].Price);
                                await Delay(5000);
                            }
                        }
                        else
                        {
                            if (!_isInRoom)
                            {
                                Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), _configurationInstance.Language.PressToEnter);
                                if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                                {
                                    _isInRoom = true;
                                    float tpEnterX = (float)_configurationInstance.Config.Rooms[i].TPEnter[0];
                                    float tpEnterY = (float)_configurationInstance.Config.Rooms[i].TPEnter[1];
                                    float tpEnterZ = (float)_configurationInstance.Config.Rooms[i].TPEnter[2];
                                    API.DoScreenFadeOut(500);
                                    await Delay(600);
                                    API.SetEntityCoords(API.PlayerPedId(), tpEnterX, tpEnterY, tpEnterZ, false, false, false, false);
                                    await Delay(100);
                                    TriggerEvent("vorp:setInstancePlayer", true);
                                    API.DoScreenFadeIn(500);
                                    await Delay(3000);
                                }
                            }
                            else
                            {
                                Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), _configurationInstance.Language.PressToLeave);
                                if (API.IsControlJustPressed(2, 0xC7B5340A)) // ENTER KEY (modifier key)
                                {
                                    _isInRoom = false;
                                    float tpLeaveX = (float)_configurationInstance.Config.Rooms[i].TPLeave[0];
                                    float tpLeaveY = (float)_configurationInstance.Config.Rooms[i].TPLeave[1];
                                    float tpLeaveZ = (float)_configurationInstance.Config.Rooms[i].TPLeave[2];
                                    API.DoScreenFadeOut(500);
                                    await Delay(600);
                                    API.SetEntityCoords(API.PlayerPedId(), tpLeaveX, tpLeaveY, tpLeaveZ, false, false, false, false);
                                    await Delay(100);
                                    TriggerEvent("vorp:setInstancePlayer", false);
                                    API.DoScreenFadeIn(500);
                                    await Delay(3000);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Tick]
        private async Task DoorLockedsTickAsync()
        {
            if (_configurationInstance.Config == null)
            {
                return;
            }

            Vector3 pCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);

            for (int i = 0; i < _configurationInstance.Config.Houses.Count; i++)
            {
                int houseId = (int)_configurationInstance.Config.Houses[i].Id;
                for (int o = 0; o < _configurationInstance.Config.Houses[i].Doors.Count; o++)
                {
                    float doorX = (float)_configurationInstance.Config.Houses[i].Doors[o][0];
                    float doorY = (float)_configurationInstance.Config.Houses[i].Doors[o][1];
                    float doorZ = (float)_configurationInstance.Config.Houses[i].Doors[o][2];
                    float doorH = (float)_configurationInstance.Config.Houses[i].Doors[o][3];

                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, doorX, doorY, doorZ, true) < 6.0f)
                    {
                        if (Houses.ContainsKey(houseId))
                        {
                            if (Houses[houseId].IsOpen)
                            {
                                //await Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), "Puerta Abierta");
                                int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16); // void SHAPETEST::START_SHAPE_TEST_BOX
                                bool hit = false;
                                Vector3 endCoords = new Vector3();
                                Vector3 surfaceNormal = new Vector3();
                                int entity = 0;
                                int result = API.GetShapeTestResult(shapeTest, ref hit, ref endCoords, ref surfaceNormal, ref entity);
                                //API.SetEntityHeading(entity, doorH);
                                API.FreezeEntityPosition(entity, false);
                                API.DoorSystemSetDoorState(entity, 0);
                            }
                            else
                            {
                                //await Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), "Puerta Cerrada");
                                int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16); // void SHAPETEST::START_SHAPE_TEST_BOX
                                bool hit = false;
                                Vector3 endCoords = new Vector3();
                                Vector3 surfaceNormal = new Vector3();
                                int entity = 0;
                                int result = API.GetShapeTestResult(shapeTest, ref hit, ref endCoords, ref surfaceNormal, ref entity);
                                API.SetEntityHeading(entity, doorH);
                                API.FreezeEntityPosition(entity, true);
                                API.DoorSystemSetDoorState(entity, 1);
                            }

                        }
                        else
                        {
                            int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16); // void SHAPETEST::START_SHAPE_TEST_BOX
                            bool hit = false;
                            Vector3 endCoords = new Vector3();
                            Vector3 surfaceNormal = new Vector3();
                            int entity = 0;
                            int result = API.GetShapeTestResult(shapeTest, ref hit, ref endCoords, ref surfaceNormal, ref entity);
                            API.SetEntityHeading(entity, doorH);
                            API.FreezeEntityPosition(entity, true);
                            API.DoorSystemSetDoorState(entity, 1);
                        }
                    }
                }
            }

            for (int i = 0; i < _configurationInstance.Config.Rooms.Count; i++)
            {
                for (int o = 0; o < _configurationInstance.Config.Rooms[i].Doors.Count; o++)
                {
                    float doorX = (float)_configurationInstance.Config.Rooms[i].Doors[o][0];
                    float doorY = (float)_configurationInstance.Config.Rooms[i].Doors[o][1];
                    float doorZ = (float)_configurationInstance.Config.Rooms[i].Doors[o][2];
                    float doorH = (float)_configurationInstance.Config.Rooms[i].Doors[o][3];

                    float distance = API.Vdist(pCoords.X, pCoords.Y, pCoords.Z, doorX, doorY, doorZ);

                    if (distance < 12.0f)
                    {
                        int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16); // void SHAPETEST::START_SHAPE_TEST_BOX
                        bool hit = false;
                        Vector3 endCoords = new Vector3();
                        Vector3 surfaceNormal = new Vector3();
                        int entity = 0;
                        int result = API.GetShapeTestResult(shapeTest, ref hit, ref endCoords, ref surfaceNormal, ref entity);
                        API.SetEntityHeading(entity, doorH);
                        API.FreezeEntityPosition(entity, true);
                        API.DoorSystemSetDoorState(entity, 1);
                    }
                }
            }
        }
        #endregion
    }
}
