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

        /// <summary>Короткий идентификатор для отображения и восстановления при десериализации.</summary>
        public string Uid => _uid;

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

        /// <summary>Восстанавливает идентичность после загрузки из файла (тот же класс, что и при сохранении).</summary>
        public void ApplyDeserializedIdentity(string uid, string modelName, double fuel)
        {
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("Uid не может быть пустым.", nameof(uid));
            _uid = uid.Length <= 5 ? uid : uid[..5];
            ModelName = modelName;
            Fuel = fuel;
        }

    }
}
