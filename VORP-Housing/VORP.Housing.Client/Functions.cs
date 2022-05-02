namespace VORP.Housing.Client
{
    class Functions : BaseScript
    {
        /// <summary>
        /// ENTITY::_GET_ENTITY_BY_DOORHASH
        /// </summary>
        /// <param name="doorHash"></param>
        /// <returns></returns>
        public static int GetEntityByDoorHash(uint doorHash)
        {
            return Function.Call<int>((Hash)0xF7424890E4A094C0, doorHash, 0);
        }

        /// <summary>
        /// OBJECT::ADD_DOOR_TO_SYSTEM_NEW
        /// </summary>
        /// <param name="doorHash"></param>
        public static void AddDoorToSystemNew(uint doorHash)
        {
            Function.Call((Hash)0xD99229FE93B46286, doorHash, true, true, false, 0, 0, false);
        }

        /// <summary>
        /// OBJECT::DOOR_SYSTEM_SET_DOOR_STATE
        /// </summary>
        /// <param name="doorHash"></param>
        /// <param name="doorState"></param>
        public static void DoorSystemSetDoorState(uint doorHash, int doorState)
        {
            Function.Call((Hash)0x6BAB9442830C7F53, doorHash, doorState);
        }

        /// <summary>
        /// OBJECT::DOOR_SYSTEM_SET_OPEN_RATIO
        /// </summary>
        /// <param name="doorHash"></param>
        public static void DoorSystemSetOpenRatio(uint doorHash)
        {
            Function.Call((Hash)0xB6E6FBA95C7324AC, doorHash, 0.0f, true);
        }

        public static void DrawTxt(string text, float x, float y, float fontScale, float fontSize, int r, int g, int b, int alpha, bool textCentred, bool shadow)
        {
            long str = Function.Call<long>(Hash._CREATE_VAR_STRING, 10, "LITERAL_STRING", text);
            Function.Call(Hash.SET_TEXT_SCALE, fontScale, fontSize);
            Function.Call(Hash._SET_TEXT_COLOR, r, g, b, alpha);
            Function.Call(Hash.SET_TEXT_CENTRE, textCentred);
            if (shadow) { Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 0, 0, 255); }
            Function.Call(Hash.SET_TEXT_FONT_FOR_CURRENT_COMMAND, 1);
            Function.Call(Hash._DISPLAY_TEXT, str, x, y);
        }

        public static void DrawTxt3D(Vector3 position, string text)
        {
            float x = 0.0F;
            float y = 0.0F;
            //Debug.WriteLine(position.X.ToString());
            API.GetScreenCoordFromWorldCoord(position.X, position.Y, position.Z, ref x, ref y);
            API.SetTextScale(0.35F, 0.35F);
            API.SetTextFontForCurrentCommand(1);
            API.SetTextColor(255, 255, 255, 215);
            long str = Function.Call<long>(Hash._CREATE_VAR_STRING, 10, "LITERAL_STRING", text);
            Function.Call((Hash)0xBE5261939FBECB8C, 1); // unknown hash name
            Function.Call((Hash)0xD79334A4BB99BAD1, str, x, y); // void HUD::_DISPLAY_TEXT
            //float factor = text.Length / 150.0F;
            //Function.Call((Hash)0xC9884ECADE94CB34, "generic_textures", "hud_menu_4a", x, y + 0.0125F, 0.015F + factor, 0.03F, 0.1F, 100, 1, 1, 190, 0); // void GRAPHICS::DRAW_SPRITE
        }
    }
}
