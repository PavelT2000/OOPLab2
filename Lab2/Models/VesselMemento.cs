using System;
using MessagePack;

namespace Lab2.Models;

/// <summary>Снимок состояния корабля для сериализации (JSON, бинарный, текстовый формат).</summary>
[MessagePackObject]
public sealed class VesselMemento
{
    [Key(0)] public string TypeName { get; set; } = "";

    [Key(1)] public string Uid { get; set; } = "";

    [Key(2)] public string ModelName { get; set; } = "";

    [Key(3)] public double Fuel { get; set; }

    [Key(4)] public int? FirePower { get; set; }

    [Key(5)] public int? Capacity { get; set; }

    public static VesselMemento FromVessel(SpaceVessel vessel)
    {
        var m = new VesselMemento
        {
            TypeName = vessel.GetType().Name,
            Uid = vessel.Uid,
            ModelName = vessel.ModelName,
            Fuel = vessel.Fuel
        };

        switch (vessel)
        {
            case CombatShip combat:
                m.FirePower = combat.FirePower;
                break;
            case TransportShip transport:
                m.Capacity = transport.Capacity;
                break;
        }

        return m;
    }

    public SpaceVessel ToVessel()
    {
        SpaceVessel vessel = TypeName switch
        {
            nameof(CargoFreighter) => new CargoFreighter(ModelName),
            nameof(Destroyer) => new Destroyer(ModelName),
            nameof(ScoutFighter) => new ScoutFighter(ModelName),
            _ => throw new FormatException($"Неизвестный тип корабля: '{TypeName}'.")
        };

        vessel.ApplyDeserializedIdentity(Uid, ModelName, Fuel);

        if (vessel is CombatShip combat && FirePower.HasValue)
            combat.FirePower = FirePower.Value;

        if (vessel is TransportShip transport && Capacity.HasValue)
            transport.Capacity = Capacity.Value;

        return vessel;
    }
}
