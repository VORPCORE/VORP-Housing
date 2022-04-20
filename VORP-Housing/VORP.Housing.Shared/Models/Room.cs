using CitizenFX.Core;

namespace VORP.Housing.Shared.Models
{
    public class Room : BaseScript
    {
        int id;
        string identifier;
        int charidentifier;
        double price;
        int maxWeight;

        public Room(int id, string identifier, int charidentifier, double price, int maxWeight)
        {
            this.Id = id;
            this.Identifier = identifier;
            this.CharIdentifier = charidentifier;
            this.Price = price;
            this.maxWeight = maxWeight;
        }

        public int Id { get => id; set => id = value; }
        public string Identifier { get => identifier; set => identifier = value; }
        public int CharIdentifier { get => charidentifier; set => charidentifier = value; }
        public double Price { get => price; set => price = value; }
        public int MaxWeight { get => maxWeight; set => maxWeight = value; }
    }
}
