using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Lab2.Models;

namespace Lab2.Serialization;

/// <summary>Собственный текстовый формат: заголовок версии и блоки VESSEL … END.</summary>
public sealed class TextFleetSerializer : IFleetSerializer
{
    private const string Header = "LAB2_FLEET_TXT_V1";
    private const string Begin = "BEGIN_VESSEL";
    private const string End = "END_VESSEL";

    public void Serialize(Stream output, IReadOnlyList<SpaceVessel> fleet)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: true);
        writer.WriteLine(Header);
        foreach (var vessel in fleet)
        {
            var m = VesselMemento.FromVessel(vessel);
            writer.WriteLine(Begin);
            writer.WriteLine($"TypeName={m.TypeName}");
            writer.WriteLine($"Uid={m.Uid}");
            writer.WriteLine($"ModelName={Escape(m.ModelName)}");
            writer.WriteLine($"Fuel={m.Fuel.ToString(CultureInfo.InvariantCulture)}");
            writer.WriteLine(m.FirePower.HasValue
                ? $"FirePower={m.FirePower.Value.ToString(CultureInfo.InvariantCulture)}"
                : "FirePower=");
            writer.WriteLine(m.Capacity.HasValue
                ? $"Capacity={m.Capacity.Value.ToString(CultureInfo.InvariantCulture)}"
                : "Capacity=");

            if (m.ExtendedData is { Count: > 0 })
            {
                foreach (var pair in m.ExtendedData)
                    writer.WriteLine($"Ext.{pair.Key}={Escape(pair.Value)}");
            }

            writer.WriteLine(End);
        }
    }

    public IReadOnlyList<SpaceVessel> Deserialize(Stream input)
    {
        using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        var lines = reader.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (lines.Length == 0 || lines[0].Trim() != Header)
            throw new FormatException($"Ожидался заголовок '{Header}'.");

        var list = new List<SpaceVessel>();
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() != Begin)
                continue;

            var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            i++;
            while (i < lines.Length && lines[i].Trim() != End)
            {
                var line = lines[i];
                var eq = line.IndexOf('=');
                if (eq > 0)
                {
                    var key = line[..eq].Trim();
                    var value = line[(eq + 1)..];
                    props[key] = Unescape(value);
                }
                i++;
            }

            list.Add(ParseVessel(props));
        }

        return list;
    }

    private static SpaceVessel ParseVessel(Dictionary<string, string> props)
    {
        string Req(string k) =>
            props.TryGetValue(k, out var v) ? v : throw new FormatException($"Отсутствует поле '{k}'.");

        var m = new VesselMemento
        {
            TypeName = Req("TypeName"),
            Uid = Req("Uid"),
            ModelName = Req("ModelName"),
            Fuel = double.Parse(Req("Fuel"), CultureInfo.InvariantCulture)
        };

        if (props.TryGetValue("FirePower", out var fp) && !string.IsNullOrWhiteSpace(fp))
            m.FirePower = int.Parse(fp, CultureInfo.InvariantCulture);

        if (props.TryGetValue("Capacity", out var cap) && !string.IsNullOrWhiteSpace(cap))
            m.Capacity = int.Parse(cap, CultureInfo.InvariantCulture);

        foreach (var pair in props)
        {
            if (!pair.Key.StartsWith("Ext.", StringComparison.OrdinalIgnoreCase))
                continue;

            m.ExtendedData ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            m.ExtendedData[pair.Key[4..]] = pair.Value;
        }

        return m.ToVessel();
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);

    private static string Unescape(string s) =>
        s.Replace("\\\\", "\0", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\0", "\\", StringComparison.Ordinal);
}
