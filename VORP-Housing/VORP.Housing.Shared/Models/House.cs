namespace VORP.Housing.Shared.Models
{
    public class House : BaseScript
    {
        public uint Id { get; set; }
        public string Interior { get; set; }
        public string Identifier { get; set; }
        public int CharIdentifier { get; set; }
        public double Price { get; set; }
        public string Furniture { get; set; }
        public bool IsOpen { get; set; }
        public bool IsOwner { get; set; }
        public int MaxWeight { get; set; }

#if SERVER
        public void BuyHouse(string identifier, int charid)
        {
            Identifier = identifier;
            CharIdentifier = charid;
            Exports["ghmattimysql"].execute($"INSERT INTO housing (id, identifier, charidentifier, furniture) VALUES (?, ?, ?, ?)", new object[] { Id, identifier, charid, "{}" });
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            int intopen = open ? 1 : 0;
            Exports["ghmattimysql"].execute($"UPDATE housing SET open=? WHERE id=?", new object[] { intopen, Id });
        }
#endif
    }
}
