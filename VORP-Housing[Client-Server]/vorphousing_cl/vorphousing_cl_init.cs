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

        }

        public static async Task LoadHouses()
        {
            await Delay(5000);

            TriggerEvent("vorp:ExecuteServerCallBack", "getHouses", new Action<string>(async (json) => {
                Houses = JsonConvert.DeserializeObject<Dictionary<int, House>>(json);
                Debug.WriteLine(Houses[34306].Interior);
            }), "");
        }

        [Tick]
        private async Task doorLockeds()
        {
            await Delay(50);
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

                    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, doorX, doorY, doorZ, true) < 20.0f)
                    {
                        if (Houses.ContainsKey(houseId))
                        {
                            if (Houses[houseId].IsOpen)
                            {
                                int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16);
                                bool hit = false;
                                Vector3 endCoords = new Vector3();
                                Vector3 surfaceNormal = new Vector3();
                                int entity = 0;
                                int result = API.GetShapeTestResult(shapeTest, ref hit, ref endCoords, ref surfaceNormal, ref entity);
                                API.SetEntityHeading(entity, doorH);
                                API.FreezeEntityPosition(entity, false);
                                API.DoorSystemSetDoorState(entity, 0);
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
