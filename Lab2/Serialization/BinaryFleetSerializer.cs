using System;
using System.Collections.Generic;
using System.IO;
using Lab2.Models;
using MessagePack;
using MessagePack.Resolvers;

namespace Lab2.Serialization;


public sealed class BinaryFleetSerializer : IFleetSerializer
{
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);

    public void Serialize(Stream output, IReadOnlyList<SpaceVessel> fleet)
    {
        var mementos = fleet.SelectToMementos();
        MessagePackSerializer.Serialize(output, mementos, Options);
    }

    public IReadOnlyList<SpaceVessel> Deserialize(Stream input)
    {
        var mementos = MessagePackSerializer.Deserialize<List<VesselMemento>>(input, Options)
                       ?? throw new FormatException("Пустой или повреждённый бинарный файл.");
        var vessels = new List<SpaceVessel>(mementos.Count);
        foreach (var m in mementos)
            vessels.Add(m.ToVessel());
        return vessels;
    }
}
