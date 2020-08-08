using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vorphousing_cl
{
    public class vorphousing_cl_init : BaseScript
    {
        public static Dictionary<int, House> Houses = new Dictionary<int, House>();

        public vorphousing_cl_init()
        {
            EventHandlers["vorp_housing:UpdateHousesStatus"] += new Action<int, string>(UpdateHouse);
            EventHandlers["vorp_housing:SetHouseOwner"] += new Action<int>(SetHouseOwner);
            EventHandlers["vorp_housing:SetDoorState"] += new Action<int, bool>(SetDoorState);
        }

        private void SetDoorState(int houseId, bool state)
        {
            Houses[houseId].IsOpen = state;
        }

        private void UpdateHouse(int houseId, string identifier)
        {
            Houses[houseId].Identifier = identifier;
        }

        private void SetHouseOwner(int houseId)
        {
            Houses[houseId].IsOwner = true;
        }

        public static async Task LoadHouses()
        {
            await Delay(5000);

            TriggerEvent("vorp:ExecuteServerCallBack", "getHouses", new Action<string>(async (json) => {
                Houses = JsonConvert.DeserializeObject<Dictionary<int, House>>(json);
            }), "");
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
                            TriggerEvent("vorp_inventory:OpenHouseInventory", "Casa", InteriorIsIn);
                            TriggerServerEvent("vorp_housing:UpdateInventoryHouse", InteriorIsIn);
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

                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, doorStatusX, doorStatusY, doorStatusZ, true) < 2.0f)
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

                    float doorStatusX = GetConfig.Config["Houses"][i]["DoorsStatus"][0].ToObject<float>();
                    float doorStatusY = GetConfig.Config["Houses"][i]["DoorsStatus"][1].ToObject<float>();
                    float doorStatusZ = GetConfig.Config["Houses"][i]["DoorsStatus"][2].ToObject<float>();

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
        }
    }
}
