using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace vorphousing_cl
{
    class Functions : BaseScript
    {
        public static async Task DrawTxt(string text, float x, float y, float fontscale, float fontsize, int r, int g, int b, int alpha, bool textcentred, bool shadow)
        {
            long str = Function.Call<long>(Hash._CREATE_VAR_STRING, 10, "LITERAL_STRING", text);
            Function.Call(Hash.SET_TEXT_SCALE, fontscale, fontsize);
            Function.Call(Hash._SET_TEXT_COLOR, r, g, b, alpha);
            Function.Call(Hash.SET_TEXT_CENTRE, textcentred);
            if (shadow) { Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 0, 0, 255); }
            Function.Call(Hash.SET_TEXT_FONT_FOR_CURRENT_COMMAND, 1);
            Function.Call(Hash._DISPLAY_TEXT, str, x, y);
        }

        public static async Task DrawTxt3D(Vector3 position, string text)
        {
            float _x = 0.0F;
            float _y = 0.0F;
            //Debug.WriteLine(position.X.ToString());
            API.GetScreenCoordFromWorldCoord(position.X, position.Y, position.Z, ref _x, ref _y);
            API.SetTextScale(0.35F, 0.35F);
            API.SetTextFontForCurrentCommand(1);
            API.SetTextColor(255, 255, 255, 215);
            long str = Function.Call<long>(Hash._CREATE_VAR_STRING, 10, "LITERAL_STRING", text);
            Function.Call((Hash)0xBE5261939FBECB8C, 1);
            Function.Call((Hash)0xD79334A4BB99BAD1, str, _x, _y);
            //float factor = text.Length / 150.0F;
            //Function.Call((Hash)0xC9884ECADE94CB34, "generic_textures", "hud_menu_4a", _x, _y + 0.0125F, 0.015F + factor, 0.03F, 0.1F, 100, 1, 1, 190, 0);
        }

    }
}
