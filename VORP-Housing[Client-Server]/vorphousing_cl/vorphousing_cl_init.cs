using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vorphousing_cl
{
    public class vorphousing_cl_init : BaseScript
    {

        [Tick]
        private async Task doorLockeds()
        {
            await Delay(50);
            if (!GetConfig.isLoaded) return;

            //Vector3 pCoords = API.GetEntityCoords(API.PlayerPedId(), true, true);
            //for (int i = 0; i < GetConfig.Config["Doors"].Count(); i++)
            //{
            //    float doorX = GetConfig.Config["Doors"][i][0].ToObject<float>();
            //    float doorY = GetConfig.Config["Doors"][i][1].ToObject<float>();
            //    float doorZ = GetConfig.Config["Doors"][i][2].ToObject<float>();
            //    float doorH = GetConfig.Config["Doors"][i][3].ToObject<float>();
            //    if (API.GetDistanceBetweenCoords(pCoords.X, pCoords.Y, pCoords.Z, doorX, doorY, doorZ, true) < 20.0f)
            //    {
            //        int shapeTest = Function.Call<int>((Hash)0xFE466162C4401D18, doorX, doorY, doorZ, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, true, 16);
            //        bool hit = false;
            //        Vector3 endCoords = new Vector3();
            //        Vector3 surfaceNormal = new Vector3();
            //        int entity = 0;
            //        int result = API.GetShapeTestResult(shapeTest, ref hit, ref endCoords, ref surfaceNormal, ref entity);
            //        API.SetEntityHeading(entity, doorH);
            //        API.FreezeEntityPosition(entity, true);
            //        API.DoorSystemSetDoorState(entity, 1);
            //    }
            //}
        }
    }
}
