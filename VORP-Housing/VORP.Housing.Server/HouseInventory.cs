using CitizenFX.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vorphousing_sv
{
    public class HouseInventory : BaseScript
    {

        public HouseInventory()
        {
            EventHandlers["vorp_housing:TakeFromHouse"] += new Action<Player, string>(TakeFromHouse);
            EventHandlers["vorp_housing:MoveToHouse"] += new Action<Player, string>(MoveToHouse);

            EventHandlers["vorp_housing:UpdateInventoryHouse"] += new Action<Player, int>(UpdateInventoryHouse);
        }

        private async void TakeFromHouse([FromSource] Player player, string jsondata)
        {
            string sid = "steam:" + player.Identifiers["steam"];
            int _source = int.Parse(player.Handle);
            dynamic UserCharacter = vorp_housing_sv_init.VORPCORE.getUser(_source).getUsedCharacter;
            int charIdentifier = UserCharacter.charIdentifier;

            JObject data = JObject.Parse(jsondata);

            if (String.IsNullOrEmpty(data["number"].ToString()))
            {
                return;
            }

            if (String.IsNullOrEmpty(data["house"].ToString()))
            {
                return;
            }

            string label = data["item"]["label"].ToString();
            string name = data["item"]["name"].ToString();
            int count = data["item"]["count"].ToObject<int>();
            int limit = data["item"]["limit"].ToObject<int>();
            int number = data["number"].ToObject<int>();
            string type = data["item"]["type"].ToString();

            int houseId = data["house"].ToObject<int>();

            if (number <= 0)
            {
                player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                return;
            }

            if (type.Contains("item_weapon"))
            {
                int weapId = data["item"]["id"].ToObject<int>();

                if (vorp_housing_sv_init.Rooms.ContainsKey(houseId))
                {
                    Exports["ghmattimysql"].execute("SELECT * FROM rooms WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                    {
                        if (result.Count == 0)
                        {
                            Debug.WriteLine($"Error House not Exist or not Buyed");
                        }
                        else
                        {
                            string inv = result[0].inventory;

                            if (!String.IsNullOrEmpty(inv))
                            {
                                JArray houseData = JArray.Parse(inv);

                                JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                if (itemFound != null)
                                {
                                    int indexItem = houseData.IndexOf(itemFound);
                                    int newcount = houseData[indexItem]["count"].ToObject<int>() - number;

                                    if (newcount < 0)
                                    {
                                        player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                                        return;
                                    }

                                    TriggerEvent("vorpCore:canCarryWeapons", int.Parse(player.Handle), number, new Action<bool>((can) =>
                                    {

                                        if (!can)
                                        {
                                            player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                                            return;
                                        }

                                        else if (newcount == 0)
                                        {
                                            houseData.RemoveAt(indexItem);
                                        }

                                        TriggerEvent("vorpCore:giveWeapon", _source, weapId, 0);
                                        Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                        JObject items = new JObject();

                                        items.Add("itemList", houseData);
                                        items.Add("action", "setSecondInventoryItems");

                                        player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                                    }));
                                }
                                else
                                {
                                    Debug.WriteLine(player.Name + "Attempt to dupe in House inventory");
                                }
                            }
                            else
                            {
                                Debug.WriteLine(player.Name + "Attempt to dupe in House inventory");
                            }
                        }

                    }));
                }
                else
                {

                    Exports["ghmattimysql"].execute("SELECT * FROM housing WHERE identifier=? AND charidentifier=? AND id=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                    {
                        if (result.Count == 0)
                        {
                            Debug.WriteLine($"Error House not Exist or not Buyed");
                        }
                        else
                        {
                            string inv = result[0].inventory;

                            if (!String.IsNullOrEmpty(inv))
                            {
                                JArray houseData = JArray.Parse(inv);

                                JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                if (itemFound != null)
                                {
                                    int indexItem = houseData.IndexOf(itemFound);
                                    int newcount = houseData[indexItem]["count"].ToObject<int>() - number;

                                    if (newcount < 0)
                                    {
                                        player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                                        return;
                                    }

                                    TriggerEvent("vorpCore:canCarryWeapons", int.Parse(player.Handle), number, new Action<dynamic>((can) =>
                                    {

                                        if (!can)
                                        {
                                            player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                                            return;
                                        }
                                        else if (newcount == 0)
                                        {
                                            houseData.RemoveAt(indexItem);
                                        }

                                        TriggerEvent("vorpCore:giveWeapon", _source, weapId, 0);
                                        Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                        JObject items = new JObject();

                                        items.Add("itemList", houseData);
                                        items.Add("action", "setSecondInventoryItems");

                                        player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                                    }));
                                }
                                else
                                {
                                    Debug.WriteLine(player.Name + "Attempt to dupe in House inventory");
                                }
                            }
                            else
                            {
                                Debug.WriteLine(player.Name + "Attempt to dupe in House inventory");
                            }
                        }

                    }));
                }
            }
            else
            {
                TriggerEvent("vorpCore:getItemCount", int.Parse(player.Handle), new Action<dynamic>((mycount) =>
                {
                    int itemc = mycount;

                    if (limit < (itemc + number) && limit != -1)
                    {
                        player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                        return;
                    }

                    if (vorp_housing_sv_init.Rooms.ContainsKey(houseId))
                    {

                        Exports["ghmattimysql"].execute("SELECT * FROM rooms WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                        {
                            if (result.Count == 0)
                            {
                                Debug.WriteLine($"Error House not Exist or not Buyed");
                            }
                            else
                            {
                                string inv = result[0].inventory;

                                if (!String.IsNullOrEmpty(inv))
                                {
                                    JArray houseData = JArray.Parse(inv);

                                    JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                    if (itemFound != null)
                                    {
                                        int indexItem = houseData.IndexOf(itemFound);

                                        int newcount = houseData[indexItem]["count"].ToObject<int>() - number;

                                        if (newcount < 0)
                                        {
                                            player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                                            return;
                                        }

                                        TriggerEvent("vorpCore:canCarryItems", int.Parse(player.Handle), number, new Action<dynamic>((can) =>
                                        {

                                            if (!can)
                                            {
                                                player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                                                return;
                                            }
                                            else if (newcount == 0)
                                            {
                                                houseData.RemoveAt(indexItem);
                                            }
                                            else
                                            {
                                                houseData[indexItem]["count"] = houseData[indexItem]["count"].ToObject<int>() - number;
                                            }

                                            TriggerEvent("vorpCore:addItem", int.Parse(player.Handle), name, number);
                                            Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                            JObject items = new JObject();

                                            items.Add("itemList", houseData);
                                            items.Add("action", "setSecondInventoryItems");

                                            player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                                        }));
                                    }
                                    else
                                    {
                                        Debug.WriteLine(player.Name + "Attempt to dupe in House inventory");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine(player.Name + "Attempt to dupe in House inventory");
                                }
                            }

                        }));
                    }
                    else
                    {

                        Exports["ghmattimysql"].execute("SELECT * FROM housing WHERE identifier=? AND charidentifier=? AND id=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                        {
                            if (result.Count == 0)
                            {
                                Debug.WriteLine($"Error House not Exist or not Buyed");
                            }
                            else
                            {
                                string inv = result[0].inventory;

                                if (!String.IsNullOrEmpty(inv))
                                {
                                    JArray houseData = JArray.Parse(inv);

                                    JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                    if (itemFound != null)
                                    {
                                        int indexItem = houseData.IndexOf(itemFound);

                                        int newcount = houseData[indexItem]["count"].ToObject<int>() - number;

                                        if (newcount < 0)
                                        {
                                            player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                                            return;
                                        }

                                        TriggerEvent("vorpCore:canCarryItems", int.Parse(player.Handle), number, new Action<dynamic>((can) =>
                                        {

                                            if (!can)
                                            {
                                                player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                                                return;
                                            }
                                            else if (newcount == 0)
                                            {
                                                houseData.RemoveAt(indexItem);
                                            }
                                            else
                                            {
                                                houseData[indexItem]["count"] = houseData[indexItem]["count"].ToObject<int>() - number;
                                            }

                                            TriggerEvent("vorpCore:addItem", int.Parse(player.Handle), name, number);
                                            Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                            JObject items = new JObject();

                                            items.Add("itemList", houseData);
                                            items.Add("action", "setSecondInventoryItems");

                                            player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                                        }));
                                    }
                                    else
                                    {
                                        Debug.WriteLine(player.Name + "Attempt to dupe in House inventory");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine(player.Name + "Attempt to dupe in House inventory");
                                }
                            }

                        }));
                    }


                }), name.Trim());
            }

        }


        private async void MoveToHouse([FromSource] Player player, string jsondata)
        {

            string sid = "steam:" + player.Identifiers["steam"];
            int _source = int.Parse(player.Handle);
            dynamic UserCharacter = vorp_housing_sv_init.VORPCORE.getUser(_source).getUsedCharacter;
            int charIdentifier = UserCharacter.charIdentifier;

            JObject data = JObject.Parse(jsondata);

            if (String.IsNullOrEmpty(data["number"].ToString()))
            {
                return;
            }

            if (String.IsNullOrEmpty(data["house"].ToString()))
            {
                return;
            }

            int houseId = data["house"].ToObject<int>();

            string type = data["item"]["type"].ToString();

            string label = data["item"]["label"].ToString();
            string name = data["item"]["name"].ToString();

            int count = data["item"]["count"].ToObject<int>();
            int number = data["number"].ToObject<int>();

            JArray itemBlackList = JArray.Parse(LoadConfig.Config["ItemsBlacklist"].ToString());
            foreach (var ibl in itemBlackList)
            {
                if (ibl.ToString().Equals(name))
                {
                    player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ItemInBlacklist"], 2500);
                    return;
                }
            }

            if ((number > count || number < 1) && !type.Contains("item_weapon"))
            {
                player.TriggerEvent("vorp:TipBottom", LoadConfig.Langs["ErrorQuantity"], 2500);
                return;
            }

            if (vorp_housing_sv_init.Rooms.ContainsKey(houseId))
            {
                Exports["ghmattimysql"].execute("SELECT * FROM rooms WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                {
                    if (result.Count == 0)
                    {
                        Debug.WriteLine($"Error House not Exist or not Buyed");
                    }
                    else
                    {
                        string inv = result[0].inventory;

                        if (!String.IsNullOrEmpty(inv))
                        {
                            JArray houseData = JArray.Parse(inv);

                            int totalWeight = 0;
                            foreach (var hd in houseData)
                            {
                                totalWeight += hd["count"].ToObject<int>();
                            }

                            int maxWeight = vorp_housing_sv_init.Rooms[houseId].MaxWeight;

                            if (type.Contains("item_weapon"))
                            {
                                number = 1; //Fix Count 0
                            }

                            if (maxWeight < (number + totalWeight))
                            {
                                player.TriggerEvent("vorp:TipBottom", string.Format(LoadConfig.Langs["MaxWeightQuantity"], totalWeight.ToString(), maxWeight.ToString()), 2500);
                                return;
                            }

                            if (type.Contains("item_weapon"))
                            {
                                data["item"]["count"] = number;
                                int weapId = data["item"]["id"].ToObject<int>();
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subWeapon", _source, weapId);
                                Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject();

                                items.Add("itemList", houseData);
                                items.Add("action", "setSecondInventoryItems");

                                player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                            }
                            else
                            {
                                JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                if (itemFound != null)
                                {
                                    int indexItem = houseData.IndexOf(itemFound);

                                    houseData[indexItem]["count"] = houseData[indexItem]["count"].ToObject<int>() + number;

                                    TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                    Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });
                                    Debug.WriteLine(houseData.ToString().Replace(Environment.NewLine, " "));
                                    JObject items = new JObject();

                                    items.Add("itemList", houseData);
                                    items.Add("action", "setSecondInventoryItems");

                                    player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());

                                }
                                else
                                {
                                    data["item"]["count"] = number;
                                    houseData.Add(data["item"]);


                                    TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                    Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                    JObject items = new JObject();

                                    items.Add("itemList", houseData);
                                    items.Add("action", "setSecondInventoryItems");

                                    player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                                }
                            }

                        }
                        else
                        {
                            JArray houseData = new JArray();

                            if (type.Contains("item_weapon"))
                            {
                              
                                data["item"]["count"] = 1;
                                int weapId = data["item"]["id"].ToObject<int>();
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subWeapon", _source, weapId);
                                Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject();

                                items.Add("itemList", houseData);
                                items.Add("action", "setSecondInventoryItems");

                                player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                            }
                            else
                            {
                                data["item"]["count"] = number;
                                houseData.Add(data["item"]);


                                TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject();

                                items.Add("itemList", houseData);
                                items.Add("action", "setSecondInventoryItems");

                                player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                            }
                        }
                    }

                }));
            }
            else // Houses
            {
                Exports["ghmattimysql"].execute("SELECT * FROM housing WHERE identifier=? AND charidentifier=? AND id=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                {
                    if (result.Count == 0)
                    {
                        Debug.WriteLine($"Error House not Exist or not Buyed");
                    }
                    else
                    {
                        string inv = result[0].inventory;

                        if (!String.IsNullOrEmpty(inv))
                        {
                            JArray houseData = JArray.Parse(inv);

                            int totalWeight = 0;
                            foreach (var hd in houseData)
                            {
                                totalWeight += hd["count"].ToObject<int>();
                            }

                            int maxWeight = vorp_housing_sv_init.Houses[(uint)houseId].MaxWeight;

                            if (type.Contains("item_weapon"))
                            {
                                number = 1; //Fix Count 0
                            }

                            if (maxWeight < (number + totalWeight))
                            {
                                player.TriggerEvent("vorp:TipBottom", string.Format(LoadConfig.Langs["MaxWeightQuantity"], totalWeight.ToString(), maxWeight.ToString()), 2500);
                                return;
                            }

                            if (type.Contains("item_weapon"))
                            {
                                data["item"]["count"] = number;
                                int weapId = data["item"]["id"].ToObject<int>();
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subWeapon", _source, weapId);
                                Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject();

                                items.Add("itemList", houseData);
                                items.Add("action", "setSecondInventoryItems");

                                player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                            }
                            else
                            {
                                JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                if (itemFound != null)
                                {
                                    int indexItem = houseData.IndexOf(itemFound);

                                    houseData[indexItem]["count"] = houseData[indexItem]["count"].ToObject<int>() + number;

                                    TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                    Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });
                                    JObject items = new JObject();

                                    items.Add("itemList", houseData);
                                    items.Add("action", "setSecondInventoryItems");

                                    player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());


                                }
                                else
                                {
                                    data["item"]["count"] = number;
                                    houseData.Add(data["item"]);


                                    TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                    Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                    JObject items = new JObject();

                                    items.Add("itemList", houseData);
                                    items.Add("action", "setSecondInventoryItems");

                                    player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                                }
                            }
                        }
                        else
                        {
                            JArray houseData = new JArray();

                            if (type.Contains("item_weapon"))
                            {
                                data["item"]["count"] = 1;
                                int weapId = data["item"]["id"].ToObject<int>();
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subWeapon", _source, weapId);
                                Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject();

                                items.Add("itemList", houseData);
                                items.Add("action", "setSecondInventoryItems");

                                player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                            }
                            else
                            {
                                data["item"]["count"] = number;
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject();

                                items.Add("itemList", houseData);
                                items.Add("action", "setSecondInventoryItems");

                                player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                            }
                        }
                    }

                }));
            }



        }

        private void UpdateInventoryHouse([FromSource] Player player, int houseId)
        {
            string sid = "steam:" + player.Identifiers["steam"];

            int _source = int.Parse(player.Handle);
            dynamic UserCharacter = vorp_housing_sv_init.VORPCORE.getUser(_source).getUsedCharacter;
            int charIdentifier = UserCharacter.charIdentifier;

            if (vorp_housing_sv_init.Rooms.ContainsKey(houseId))
            {

                Exports["ghmattimysql"].execute("SELECT * FROM rooms WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                {
                    if (result.Count != 0)
                    {
                        JObject items = new JObject();

                        string inv = result[0].inventory;

                        if (String.IsNullOrEmpty(inv))
                        {
                            items.Add("itemList", "[]");
                            items.Add("action", "setSecondInventoryItems");

                            player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                        }
                        else
                        {
                            JArray data = JArray.Parse(inv);
                            items.Add("itemList", data);
                            items.Add("action", "setSecondInventoryItems");

                            player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                        }
                    }

                }));
            }
            else
            {

                Exports["ghmattimysql"].execute("SELECT * FROM housing WHERE identifier=? AND charidentifier=? AND id=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                {
                    if (result.Count != 0)
                    {
                        JObject items = new JObject();

                        string inv = result[0].inventory;

                        if (String.IsNullOrEmpty(inv))
                        {
                            items.Add("itemList", "[]");
                            items.Add("action", "setSecondInventoryItems");

                            player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                        }
                        else
                        {
                            JArray data = JArray.Parse(inv);
                            items.Add("itemList", data);
                            items.Add("action", "setSecondInventoryItems");

                            player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                        }
                    }

                }));
            }

        }

    }
}
