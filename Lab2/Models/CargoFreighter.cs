using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab2.Models
{
    public sealed class CargoFreighter :TransportShip
    {
        private string[] _cargoLog = new string[10];
        public CargoFreighter(string  name) :base(name,2000)
        {

        }
        public string this[int index]
        {
            get => _cargoLog[index];
            set => _cargoLog[index] = value;
        }

    }
}
