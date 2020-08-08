using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vorphousing_cl
{
    public class House
    {
        int id;
        string interior;
        string identifier;
        double price;
        string furniture;
        bool isOpen;
        bool isOwner;
        int maxWeight;

        public House(int id, string interior, string identifier, double price, string furniture, bool isOpen, int maxWeight)
        {
            this.Id = id;
            this.Interior = interior;
            this.Identifier = identifier;
            this.Price = price;
            this.Furniture = furniture;
            this.isOpen = isOpen;
            this.isOwner = false;
            this.maxWeight = maxWeight;
        }

        public int Id { get => id; set => id = value; }
        public string Interior { get => interior; set => interior = value; }
        public string Identifier { get => identifier; set => identifier = value; }
        public double Price { get => price; set => price = value; }
        public string Furniture { get => furniture; set => furniture = value; }
        public bool IsOpen { get => isOpen; set => isOpen = value; }
        public bool IsOwner { get => isOwner; set => isOwner = value; }
        public int MaxWeight { get => maxWeight; set => maxWeight = value; }
    }
}
