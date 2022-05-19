using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VORP.Housing.Shared.Diagnostics;

namespace vorphousing_cl
{
    public class vorphousing_cl_init : BaseScript
    {
        public static Dictionary<int, House> Houses = new Dictionary<int, House>();
        public static Dictionary<int, Room> Rooms = new Dictionary<int, Room>();

        public static bool isInInterior = false;
        public static bool isInRoom = false;

        public vorphousing_cl_init()
        {
            EventHandlers["vorp_housing:UpdateHousesStatus"] += new Action<int, string>(UpdateHouse);
            EventHandlers["vorp_housing:UpdateRoomsStatus"] += new Action<int, string>(UpdateRoom);
            EventHandlers["vorp_housing:SetHouseOwner"] += new Action<int>(SetHouseOwner);
            EventHandlers["vorp_housing:SetDoorState"] += new Action<int, bool>(SetDoorState);
            EventHandlers["vorp_housing:ListHouses"] += new Action<string>(ListHouses);
            EventHandlers["vorp_housing:ListRooms"] += new Action<string>(ListRooms);
            EventHandlers["vorp:SelectedCharacter"] += new Action<int>((charId) =>
            {
                TriggerServerEvent("vorp_housing:getHouses", charId);
                TriggerServerEvent("vorp_housing:getRooms", charId);
            });
        }

        private void ListHouses(string json)
        {
            Houses = JsonConvert.DeserializeObject<Dictionary<int, House>>(json);
            SetBlips();
        }

        private void ListRooms(string json)
        {
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

        public static async Task SetBlips()
        {
            try
            {
                //249721687
                foreach (var h in GetConfig.Config["Houses"])
                {
                    int _blip = Function.Call<int>((Hash)0x554D9D53F696D002, 1664425300, h["DoorsStatus"][0].ToObject<float>(), h["DoorsStatus"][1].ToObject<float>(), h["DoorsStatus"][2].ToObject<float>());
                    if (Houses.ContainsKey(h["Id"].ToObject<int>()))
                    {
                        if ((String.IsNullOrEmpty(Houses[h["Id"].ToObject<int>()].Identifier)))
                        {
                            Function.Call((Hash)0x74F74D3207ED525C, _blip, 249721687, 1);
                        }
                        else
                        {
                            Function.Call((Hash)0x74F74D3207ED525C, _blip, -2024635066, 1);
                        }
                        Function.Call((Hash)0x9CB1A1623062F402, _blip, h["Name"].ToString());

                        Function.Call((Hash)0x174D0AAB11CED739, h["Id"].ToObject<int>(), h["InteriorName"].ToString()); // Load Entity Interior
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Server.Init.SetBlips(): {ex.Message}");
            }
        }

        [Tick]
        public async Task useInteriorComps()
        {
            if (!GetConfig.isLoaded) return;

            Vector3 pCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);
            int InteriorIsIn = API.GetInteriorFromEntity(API.PlayerPedId());
            if (Houses.ContainsKey(InteriorIsIn))
            {
                if (Houses[InteriorIsIn].IsOwner)
                {
                    JObject houseIsIn = GetConfig.Config["Houses"].FirstOrDefault(x => x["Id"].ToObject<int>() == InteriorIsIn).ToObject<JObject>();
                    float invX = houseIsIn["Inventory"][0].ToObject<float>();
                    float invY = houseIsIn["Inventory"][1].ToObject<float>();
                    float invZ = houseIsIn["Inventory"][2].ToObject<float>();
                    float invR = houseIsIn["Inventory"][3].ToObject<float>();
                    
                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, invX, invY, invZ, true) <= invR)
                    {
                        await Functions.DrawTxt(GetConfig.Langs["OpenInventory"], 0.5f, 0.9f, 0.7f, 0.7f, 255, 255, 255, 255, true, true);
                        if (API.IsControlJustPressed(2, 0xC7B5340A))
                        {
                            TriggerEvent("vorp_inventory:OpenHouseInventory", houseIsIn["Name"].ToString(), InteriorIsIn);
                            TriggerServerEvent("vorp_housing:UpdateInventoryHouse", InteriorIsIn);
                        }
                    }
                }
            }

            for (int i = 0; i < GetConfig.Config["Rooms"].Count(); i++)
            {
                int roomId = GetConfig.Config["Rooms"][i]["Id"].ToObject<int>();

                float invX = GetConfig.Config["Rooms"][i]["Inventory"][0].ToObject<float>();
                float invY = GetConfig.Config["Rooms"][i]["Inventory"][1].ToObject<float>();
                float invZ = GetConfig.Config["Rooms"][i]["Inventory"][2].ToObject<float>();
                float invR = GetConfig.Config["Rooms"][i]["Inventory"][3].ToObject<float>();

                if (Rooms.ContainsKey(roomId))
                {
                    if (!String.IsNullOrEmpty(Rooms[roomId].Identifier))
                    {
                        if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, invX, invY, invZ, true) < invR)
                        {
                            await Functions.DrawTxt(GetConfig.Langs["OpenInventory"], 0.5f, 0.9f, 0.7f, 0.7f, 255, 255, 255, 255, true, true);
                            if (API.IsControlJustPressed(2, 0xC7B5340A))
                            {
                                TriggerEvent("vorp_inventory:OpenHouseInventory", GetConfig.Config["Rooms"][i]["Name"].ToString(), roomId);
                                TriggerServerEvent("vorp_housing:UpdateInventoryHouse", roomId);
                            }
                        }
                    }
                }

            }

        }

        [Tick]
        private async Task changeStatus()
        {
            if (!GetConfig.isLoaded) return;
            
            for (int i = 0; i < GetConfig.Config["Houses"].Count(); i++)
            {
                int houseId = GetConfig.Config["Houses"][i]["Id"].ToObject<int>();

                float doorStatusX = GetConfig.Config["Houses"][i]["DoorsStatus"][0].ToObject<float>();
                float doorStatusY = GetConfig.Config["Houses"][i]["DoorsStatus"][1].ToObject<float>();
                float doorStatusZ = GetConfig.Config["Houses"][i]["DoorsStatus"][2].ToObject<float>();

                if (Houses.ContainsKey(houseId))
                {
                    Vector3 pCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);

                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, doorStatusX, doorStatusY, doorStatusZ, true) < 2.5f)
                    {
                        if (String.IsNullOrEmpty(Houses[houseId].Identifier))
                        {
                            await Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), string.Format(GetConfig.Langs["PressToBuy"], GetConfig.Config["Houses"][i]["Price"].ToString()));
                            if (API.IsControlJustPressed(2, 0xC7B5340A))
                            {
                                TriggerServerEvent("vorp_housing:BuyHouse", houseId, GetConfig.Config["Houses"][i]["Price"].ToObject<double>());
                                await Delay(5000);
                            }
                        }

                        if (Houses[houseId].IsOwner)
                        {
                            if (Houses[houseId].IsOpen)
                            {
                                await Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), GetConfig.Langs["PressToClose"]);

                                if (API.IsControlJustPressed(2, 0xC7B5340A))
                                {
                                    Houses[houseId].IsOpen = false;
                                    TriggerServerEvent("vorp_housing:changeDoorState", houseId, false);
                                    await Delay(3000);
                                }
                            }
                            else
                            {
                                await Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), GetConfig.Langs["PressToOpen"]);

                                if (API.IsControlJustPressed(2, 0xC7B5340A))
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

            for (int i = 0; i < GetConfig.Config["Rooms"].Count(); i++)
            {
                int roomId = GetConfig.Config["Rooms"][i]["Id"].ToObject<int>();

                float doorStatusX = GetConfig.Config["Rooms"][i]["DoorsStatus"][0].ToObject<float>();
                float doorStatusY = GetConfig.Config["Rooms"][i]["DoorsStatus"][1].ToObject<float>();
                float doorStatusZ = GetConfig.Config["Rooms"][i]["DoorsStatus"][2].ToObject<float>();

                if (Rooms.ContainsKey(roomId))
                {
                    Vector3 pCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);

                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, doorStatusX, doorStatusY, doorStatusZ, true) < 2.5f)
                    {
                        if (String.IsNullOrEmpty(Rooms[roomId].Identifier))
                        {
                            await Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), string.Format(GetConfig.Langs["PressToBuyRoom"], GetConfig.Config["Rooms"][i]["Price"].ToString()));
                            if (API.IsControlJustPressed(2, 0xC7B5340A))
                            {
                                TriggerServerEvent("vorp_housing:BuyRoom", roomId, GetConfig.Config["Rooms"][i]["Price"].ToObject<double>());
                                await Delay(5000);
                            }
                        }
                        else
                        {
                            if (!isInRoom)
                            {
                                await Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), GetConfig.Langs["PressToEnter"]);
                                if (API.IsControlJustPressed(2, 0xC7B5340A))
                                {
                                    isInRoom = true;
                                    float tpEnterX = GetConfig.Config["Rooms"][i]["TPEnter"][0].ToObject<float>();
                                    float tpEnterY = GetConfig.Config["Rooms"][i]["TPEnter"][1].ToObject<float>();
                                    float tpEnterZ = GetConfig.Config["Rooms"][i]["TPEnter"][2].ToObject<float>();
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
                                await Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), GetConfig.Langs["PressToLeave"]);
                                if (API.IsControlJustPressed(2, 0xC7B5340A))
                                {
                                    isInRoom = false;
                                    float tpLeaveX = GetConfig.Config["Rooms"][i]["TPLeave"][0].ToObject<float>();
                                    float tpLeaveY = GetConfig.Config["Rooms"][i]["TPLeave"][1].ToObject<float>();
                                    float tpLeaveZ = GetConfig.Config["Rooms"][i]["TPLeave"][2].ToObject<float>();
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
        private async Task doorLockeds()
        {
            if (!GetConfig.isLoaded) return;

            Vector3 pCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);

            for (int i = 0; i < GetConfig.Config["Houses"].Count(); i++)
            {
                int houseId = GetConfig.Config["Houses"][i]["Id"].ToObject<int>();
                for (int o = 0; o < GetConfig.Config["Houses"][i]["Doors"].Count(); o++)
                {
                    float doorX = GetConfig.Config["Houses"][i]["Doors"][o][0].ToObject<float>();
                    float doorY = GetConfig.Config["Houses"][i]["Doors"][o][1].ToObject<float>();
                    float doorZ = GetConfig.Config["Houses"][i]["Doors"][o][2].ToObject<float>();
                    float doorH = GetConfig.Config["Houses"][i]["Doors"][o][3].ToObject<float>();

                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, doorX, doorY, doorZ, true) < 6.0f)
                    {
                        if (Houses.ContainsKey(houseId))
                        {
                            if (Houses[houseId].IsOpen)
                            {
                                //await Functions.DrawTxt3D(new Vector3(doorStatusX, doorStatusY, doorStatusZ), "Puerta Abierta");
                                int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16);
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
                                int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16);
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
                            int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16);
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

            for (int i = 0; i < GetConfig.Config["Rooms"].Count(); i++)
            {
                for (int o = 0; o < GetConfig.Config["Rooms"][i]["Doors"].Count(); o++)
                {
                    float doorX = GetConfig.Config["Rooms"][i]["Doors"][o][0].ToObject<float>();
                    float doorY = GetConfig.Config["Rooms"][i]["Doors"][o][1].ToObject<float>();
                    float doorZ = GetConfig.Config["Rooms"][i]["Doors"][o][2].ToObject<float>();
                    float doorH = GetConfig.Config["Rooms"][i]["Doors"][o][3].ToObject<float>();

                    float distance = API.Vdist(pCoords.X, pCoords.Y, pCoords.Z, doorX, doorY, doorZ);

                    if (distance < 12.0f)
                    {
                            int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16);
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

        }
    
}
