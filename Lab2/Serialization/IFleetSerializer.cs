using System.Collections.Generic;
using System.IO;
using Lab2.Models;

namespace Lab2.Serialization;

public interface IFleetSerializer
{
    void Serialize(Stream output, IReadOnlyList<SpaceVessel> fleet);

    IReadOnlyList<SpaceVessel> Deserialize(Stream input);
}
