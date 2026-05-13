using System;
using System.Collections.Generic;
using Lab2.Models;

namespace Lab2.Factories
{
    public static class VesselFactory
    {

        private static readonly Dictionary<string, Func<string, SpaceVessel>> _registry =
            new Dictionary<string, Func<string, SpaceVessel>>
            {
                { "Destroyer", name => new Destroyer(name) },
                { "Cargo", name => new CargoFreighter(name) },
                { "Scout", name=> new ScoutFighter(name) }
            };

        public static SpaceVessel CreateVessel(string type, string name)
        {
            if (_registry.TryGetValue(type, out var constructor))
            {
                return constructor(name);
            }

            throw new ArgumentException($"Тип корабля '{type}' не зарегистрирован в верфи.");
        }

    }
}




