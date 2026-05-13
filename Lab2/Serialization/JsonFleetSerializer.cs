using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lab2.Models;
using Newtonsoft.Json;

namespace Lab2.Serialization;

/// <summary>Сериализация списка снимков через Newtonsoft.Json (NuGet).</summary>
public sealed class JsonFleetSerializer : IFleetSerializer
{
    private static readonly JsonSerializer Serializer = new()
    {
        Formatting = Formatting.Indented,
        Culture = System.Globalization.CultureInfo.InvariantCulture
    };

    public void Serialize(Stream output, IReadOnlyList<SpaceVessel> fleet)
    {
        var mementos = fleet.SelectToMementos();
        using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 4096, leaveOpen: true);
        using var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
        Serializer.Serialize(jsonWriter, mementos);
    }

    public IReadOnlyList<SpaceVessel> Deserialize(Stream input)
    {
        using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
        using var jsonReader = new JsonTextReader(reader);
        var mementos = Serializer.Deserialize<List<VesselMemento>>(jsonReader)
                       ?? throw new FormatException("Пустой или некорректный JSON.");
        var vessels = new List<SpaceVessel>(mementos.Count);
        foreach (var m in mementos)
            vessels.Add(m.ToVessel());
        return vessels;
    }
}

internal static class FleetSerializationLinq
{
    public static List<VesselMemento> SelectToMementos(this IReadOnlyList<SpaceVessel> fleet)
    {
        var list = new List<VesselMemento>(fleet.Count);
        foreach (var v in fleet)
            list.Add(VesselMemento.FromVessel(v));
        return list;
    }
}
