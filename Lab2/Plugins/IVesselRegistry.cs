using System;
using Lab2.Models;

namespace Lab2.Plugins;

public interface IVesselRegistry
{
    void RegisterVessel(
        string typeKey,
        string displayName,
        Type vesselType,
        Func<string, SpaceVessel> factory,
        Action<SpaceVessel, VesselMemento>? captureExtra = null,
        Action<SpaceVessel, VesselMemento>? applyExtra = null,
        params VesselUiAction[] actions);
}
