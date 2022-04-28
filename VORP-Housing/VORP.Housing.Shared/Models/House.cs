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
    }
}
