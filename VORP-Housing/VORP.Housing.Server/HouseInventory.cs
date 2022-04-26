using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using VORP.Housing.Shared;

namespace VORP.Housing.Server
{
    public class HouseInventory : BaseScript
    {
        private readonly ConfigurationSingleton _configurationInstance = ConfigurationSingleton.Instance;

        public HouseInventory()
        {
            EventHandlers["vorp_housing:TakeFromHouse"] += new Action<Player, string>(TakeFromHouse);
            EventHandlers["vorp_housing:MoveToHouse"] += new Action<Player, string>(MoveToHouse);
            EventHandlers["vorp_housing:UpdateInventoryHouse"] += new Action<Player, int>(UpdateInventoryHouse);
        }

        #region Private Methods
        private void TakeFromHouse([FromSource] Player player, string jsonData)
        {
            string sid = "steam:" + player.Identifiers["steam"];
            int source = int.Parse(player.Handle);
            dynamic userCharacter = Init.VORPCORE.getUser(source).getUsedCharacter;
            int charIdentifier = userCharacter.charIdentifier;

            JObject data = JObject.Parse(jsonData);

            if (string.IsNullOrEmpty(data["number"].ToString()))
            {
                return;
            }

            if (string.IsNullOrEmpty(data["house"].ToString()))
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
                player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                return;
            }

            if (type.Contains("item_weapon"))
            {
                int weaponId = data["item"]["id"].ToObject<int>();

                if (Init.Rooms.ContainsKey(houseId))
                {
                    Exports["ghmattimysql"].execute("SELECT * FROM rooms WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                    {
                        if (result.Count == 0)
                        {
                            Debug.WriteLine($"Error House not Exist or not Buyed");
                        }
                        else
                        {
                            string inventory = result[0].inventory;

                            if (!string.IsNullOrEmpty(inventory))
                            {
                                JArray houseData = JArray.Parse(inventory);

                                JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                if (itemFound != null)
                                {
                                    int indexItem = houseData.IndexOf(itemFound);
                                    int newCount = houseData[indexItem]["count"].ToObject<int>() - number;

                                    if (newCount < 0)
                                    {
                                        player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                                        return;
                                    }

                                    TriggerEvent("vorpCore:canCarryWeapons", int.Parse(player.Handle), number, new Action<bool>((allowed) =>
                                    {

                                        if (!allowed)
                                        {
                                            player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                                            return;
                                        }

                                        else if (newCount == 0)
                                        {
                                            houseData.RemoveAt(indexItem);
                                        }

                                        TriggerEvent("vorpCore:giveWeapon", source, weaponId, 0);
                                        Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                        JObject items = new JObject
                                        {
                                            { "itemList", houseData },
                                            { "action", "setSecondInventoryItems" }
                                        };

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
                            string inventory = result[0].inventory;

                            if (!string.IsNullOrEmpty(inventory))
                            {
                                JArray houseData = JArray.Parse(inventory);

                                JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                if (itemFound != null)
                                {
                                    int indexItem = houseData.IndexOf(itemFound);
                                    int newCount = houseData[indexItem]["count"].ToObject<int>() - number;

                                    if (newCount < 0)
                                    {
                                        player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                                        return;
                                    }

                                    TriggerEvent("vorpCore:canCarryWeapons", int.Parse(player.Handle), number, new Action<dynamic>((allowed) =>
                                    {

                                        if (!allowed)
                                        {
                                            player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                                            return;
                                        }
                                        else if (newCount == 0)
                                        {
                                            houseData.RemoveAt(indexItem);
                                        }

                                        TriggerEvent("vorpCore:giveWeapon", source, weaponId, 0);
                                        Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                        JObject items = new JObject
                                        {
                                            { "itemList", houseData },
                                            { "action", "setSecondInventoryItems" }
                                        };

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
                TriggerEvent("vorpCore:getItemCount", int.Parse(player.Handle), new Action<dynamic>((myCount) =>
                {
                    int itemCount = myCount;

                    if (limit < (itemCount + number) && limit != -1)
                    {
                        player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                        return;
                    }

                    if (Init.Rooms.ContainsKey(houseId))
                    {
                        Exports["ghmattimysql"].execute("SELECT * FROM rooms WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                        {
                            if (result.Count == 0)
                            {
                                Debug.WriteLine($"Error House not Exist or not Buyed");
                            }
                            else
                            {
                                string inventory = result[0].inventory;

                                if (!string.IsNullOrEmpty(inventory))
                                {
                                    JArray houseData = JArray.Parse(inventory);

                                    JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                    if (itemFound != null)
                                    {
                                        int indexItem = houseData.IndexOf(itemFound);

                                        int newCount = houseData[indexItem]["count"].ToObject<int>() - number;

                                        if (newCount < 0)
                                        {
                                            player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                                            return;
                                        }

                                        TriggerEvent("vorpCore:canCarryItems", int.Parse(player.Handle), number, new Action<dynamic>((allowed) =>
                                        {

                                            if (!allowed)
                                            {
                                                player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                                                return;
                                            }
                                            else if (newCount == 0)
                                            {
                                                houseData.RemoveAt(indexItem);
                                            }
                                            else
                                            {
                                                houseData[indexItem]["count"] = houseData[indexItem]["count"].ToObject<int>() - number;
                                            }

                                            TriggerEvent("vorpCore:addItem", int.Parse(player.Handle), name, number);
                                            Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                            JObject items = new JObject
                                            {
                                                { "itemList", houseData },
                                                { "action", "setSecondInventoryItems" }
                                            };

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
                                string inventory = result[0].inventory;

                                if (!string.IsNullOrEmpty(inventory))
                                {
                                    JArray houseData = JArray.Parse(inventory);

                                    JToken itemFound = houseData.FirstOrDefault(x => x["name"].ToString().Equals(name));

                                    if (itemFound != null)
                                    {
                                        int indexItem = houseData.IndexOf(itemFound);

                                        int newCount = houseData[indexItem]["count"].ToObject<int>() - number;

                                        if (newCount < 0)
                                        {
                                            player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                                            return;
                                        }

                                        TriggerEvent("vorpCore:canCarryItems", int.Parse(player.Handle), number, new Action<dynamic>((allowed) =>
                                        {

                                            if (!allowed)
                                            {
                                                player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                                                return;
                                            }
                                            else if (newCount == 0)
                                            {
                                                houseData.RemoveAt(indexItem);
                                            }
                                            else
                                            {
                                                houseData[indexItem]["count"] = houseData[indexItem]["count"].ToObject<int>() - number;
                                            }

                                            TriggerEvent("vorpCore:addItem", int.Parse(player.Handle), name, number);
                                            Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                            JObject items = new JObject
                                            {
                                                { "itemList", houseData },
                                                { "action", "setSecondInventoryItems" }
                                            };

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

        private void MoveToHouse([FromSource] Player player, string jsonData)
        {

            string sid = "steam:" + player.Identifiers["steam"];
            int source = int.Parse(player.Handle);
            dynamic userCharacter = Init.VORPCORE.getUser(source).getUsedCharacter;
            int charIdentifier = userCharacter.charIdentifier;

            JObject data = JObject.Parse(jsonData);

            if (string.IsNullOrEmpty(data["number"].ToString()))
            {
                return;
            }

            if (string.IsNullOrEmpty(data["house"].ToString()))
            {
                return;
            }

            int houseId = data["house"].ToObject<int>();

            string type = data["item"]["type"].ToString();

            string label = data["item"]["label"].ToString();
            string name = data["item"]["name"].ToString();

            int count = data["item"]["count"].ToObject<int>();
            int number = data["number"].ToObject<int>();

            JArray itemBlackList = JArray.Parse(_configurationInstance.Config.ItemsBlacklist.ToString());
            foreach (var blackListItem in itemBlackList)
            {
                if (blackListItem.ToString().Equals(name))
                {
                    player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ItemInBlacklist, 2500);
                    return;
                }
            }

            if ((number > count || number < 1) && !type.Contains("item_weapon"))
            {
                player.TriggerEvent("vorp:TipBottom", _configurationInstance.Language.ErrorQuantity, 2500);
                return;
            }

            if (Init.Rooms.ContainsKey(houseId))
            {
                Exports["ghmattimysql"].execute("SELECT * FROM rooms WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                {
                    if (result.Count == 0)
                    {
                        Debug.WriteLine($"Error House not Exist or not Buyed");
                    }
                    else
                    {
                        string inventory = result[0].inventory;

                        if (!string.IsNullOrEmpty(inventory))
                        {
                            JArray houseData = JArray.Parse(inventory);

                            int totalWeight = 0;
                            foreach (var hd in houseData)
                            {
                                totalWeight += hd["count"].ToObject<int>();
                            }

                            int maxWeight = Init.Rooms[houseId].MaxWeight;

                            if (type.Contains("item_weapon"))
                            {
                                number = 1; // fix count 0
                            }

                            if (maxWeight < (number + totalWeight))
                            {
                                player.TriggerEvent("vorp:TipBottom", string.Format(_configurationInstance.Language.MaxWeightQuantity, totalWeight.ToString(), maxWeight.ToString()), 2500);
                                return;
                            }

                            if (type.Contains("item_weapon"))
                            {
                                data["item"]["count"] = number;
                                int weaponId = data["item"]["id"].ToObject<int>();
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subWeapon", source, weaponId);
                                Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject
                                {
                                    { "itemList", houseData },
                                    { "action", "setSecondInventoryItems" }
                                };

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
                                    JObject items = new JObject
                                    {
                                        { "itemList", houseData },
                                        { "action", "setSecondInventoryItems" }
                                    };

                                    player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                                }
                                else
                                {
                                    data["item"]["count"] = number;
                                    houseData.Add(data["item"]);
                                    
                                    TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                    Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                    JObject items = new JObject
                                    {
                                        { "itemList", houseData },
                                        { "action", "setSecondInventoryItems" }
                                    };

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

                                TriggerEvent("vorpCore:subWeapon", source, weapId);
                                Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject
                                {
                                    { "itemList", houseData },
                                    { "action", "setSecondInventoryItems" }
                                };

                                player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                            }
                            else
                            {
                                data["item"]["count"] = number;
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                Exports["ghmattimysql"].execute("UPDATE rooms SET inventory=? WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject
                                {
                                    { "itemList", houseData },
                                    { "action", "setSecondInventoryItems" }
                                };

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
                        string inventory = result[0].inventory;

                        if (!string.IsNullOrEmpty(inventory))
                        {
                            JArray houseData = JArray.Parse(inventory);

                            int totalWeight = 0;
                            foreach (var hd in houseData)
                            {
                                totalWeight += hd["count"].ToObject<int>();
                            }

                            int maxWeight = Init.Houses[(uint)houseId].MaxWeight;

                            if (type.Contains("item_weapon"))
                            {
                                number = 1; // fix count 0
                            }

                            if (maxWeight < (number + totalWeight))
                            {
                                player.TriggerEvent("vorp:TipBottom", string.Format(_configurationInstance.Language.MaxWeightQuantity, totalWeight.ToString(), maxWeight.ToString()), 2500);
                                return;
                            }

                            if (type.Contains("item_weapon"))
                            {
                                data["item"]["count"] = number;
                                int weaponId = data["item"]["id"].ToObject<int>();
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subWeapon", source, weaponId);
                                Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject
                                {
                                    { "itemList", houseData },
                                    { "action", "setSecondInventoryItems" }
                                };

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
                                    JObject items = new JObject
                                    {
                                        { "itemList", houseData },
                                        { "action", "setSecondInventoryItems" }
                                    };

                                    player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                                }
                                else
                                {
                                    data["item"]["count"] = number;
                                    houseData.Add(data["item"]);

                                    TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                    Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                    JObject items = new JObject
                                    {
                                        { "itemList", houseData },
                                        { "action", "setSecondInventoryItems" }
                                    };

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
                                int weaponId = data["item"]["id"].ToObject<int>();
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subWeapon", source, weaponId);
                                Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject
                                {
                                    { "itemList", houseData },
                                    { "action", "setSecondInventoryItems" }
                                };

                                player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                            }
                            else
                            {
                                data["item"]["count"] = number;
                                houseData.Add(data["item"]);

                                TriggerEvent("vorpCore:subItem", int.Parse(player.Handle), name, number);
                                Exports["ghmattimysql"].execute("UPDATE housing SET inventory=? WHERE identifier=? AND charidentifier=? AND id=?", new object[] { houseData.ToString().Replace(Environment.NewLine, " "), sid, charIdentifier, houseId });

                                JObject items = new JObject
                                {
                                    { "itemList", houseData },
                                    { "action", "setSecondInventoryItems" }
                                };

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

            int source = int.Parse(player.Handle);
            dynamic userCharacter = Init.VORPCORE.getUser(source).getUsedCharacter;
            int charIdentifier = userCharacter.charIdentifier;

            if (Init.Rooms.ContainsKey(houseId))
            {
                Exports["ghmattimysql"].execute("SELECT * FROM rooms WHERE identifier=? AND charidentifier=? AND interiorId=?", new object[] { sid, charIdentifier, houseId }, new Action<dynamic>((result) =>
                {
                    if (result.Count != 0)
                    {
                        JObject items = new JObject();

                        string inventory = result[0].inventory;

                        if (string.IsNullOrEmpty(inventory))
                        {
                            items.Add("itemList", "[]");
                            items.Add("action", "setSecondInventoryItems");

                            player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                        }
                        else
                        {
                            JArray data = JArray.Parse(inventory);
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

                        string inventory = result[0].inventory;

                        if (string.IsNullOrEmpty(inventory))
                        {
                            items.Add("itemList", "[]");
                            items.Add("action", "setSecondInventoryItems");

                            player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                        }
                        else
                        {
                            JArray data = JArray.Parse(inventory);
                            items.Add("itemList", data);
                            items.Add("action", "setSecondInventoryItems");

                            player.TriggerEvent("vorp_inventory:ReloadHouseInventory", items.ToString());
                        }
                    }
                }));
            }
        }
        #endregion
    }
}
