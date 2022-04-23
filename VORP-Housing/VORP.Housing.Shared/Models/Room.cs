namespace VORP.Housing.Shared.Models
{
    public class Room : BaseScript
    {
        public int Id { get; set; }
        public string Identifier { get; set; }
        public int CharIdentifier { get; set; }
        public double Price { get; set; }
        public int MaxWeight { get; set; }
    }
}
