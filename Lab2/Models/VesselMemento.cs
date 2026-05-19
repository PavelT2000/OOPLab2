using System;
using System.Collections.Generic;
using MessagePack;

namespace Lab2.Models;

[MessagePackObject]
public sealed class VesselMemento
{
    [Key(0)] public string TypeName { get; set; } = "";

    [Key(1)] public string Uid { get; set; } = "";

    [Key(2)] public string ModelName { get; set; } = "";

    [Key(3)] public double Fuel { get; set; }

    [Key(4)] public int? FirePower { get; set; }

    [Key(5)] public int? Capacity { get; set; }

    [Key(6)] public Dictionary<string, string>? ExtendedData { get; set; }

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

        Plugins.VesselPluginRegistry.Instance.CaptureExtra(vessel, m);
        return m;
    }

    public SpaceVessel ToVessel() => Plugins.VesselPluginRegistry.Instance.Restore(this);

    public void SetExtended(string key, string value)
    {
        ExtendedData ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ExtendedData[key] = value;
    }

    public bool TryGetExtended(string key, out string value)
    {
        if (ExtendedData != null && ExtendedData.TryGetValue(key, out var stored))
        {
            value = stored;
            return true;
        }

        value = "";
        return false;
    }
}
