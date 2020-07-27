using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vorphousing_sv
{
    public class House
    {
        int id;
        string interior;
        string identifier;
        double price;
        string furtniture;
        bool isOpen;

        public House(int id, string interior, string identifier, double price, string furtniture, bool isOpen)
        {
            this.Id = id;
            this.Interior = interior;
            this.Identifier = identifier;
            this.Price = price;
            this.Furtniture = furtniture;
            this.isOpen = isOpen;
        }

        public int Id { get => id; set => id = value; }
        public string Interior { get => interior; set => interior = value; }
        public string Identifier { get => identifier; set => identifier = value; }
        public double Price { get => price; set => price = value; }
        public string Furtniture { get => furtniture; set => furtniture = value; }
        public bool IsOpen { get => isOpen; set => isOpen = value; }
    }
}
