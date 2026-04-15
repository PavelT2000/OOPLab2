using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab2.Models
{
    public abstract class SpaceVessel
    {
        private string _uid;
        private double _fuel;
        
        public string ModelName { get; protected set; }

        public double Fuel
        {
            get => _fuel;
            set
            {
                if (value < 0) throw new ArgumentException("Fuel cannot be negative!");
                _fuel = value;
            }
        }

        protected SpaceVessel(string model, double initialFuel)
        {
            _uid = Guid.NewGuid().ToString().Substring(0, 5);
            ModelName = model;
            Fuel = initialFuel;
        }
        ~SpaceVessel()
        {
            Console.WriteLine($"Object: {ModelName} is deleted from memory");
        }
        public virtual string GetStatus()
        {
            return $"[{_uid}] Ship {ModelName}: Fuel {Fuel}%";
        }
        public abstract void ExecuteMission();

    }
}
