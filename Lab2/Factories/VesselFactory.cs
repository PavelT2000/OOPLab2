using Lab2.Plugins;

namespace Lab2.Factories;

public static class VesselFactory
{
    public static Models.SpaceVessel CreateVessel(string type, string name) =>
        VesselPluginRegistry.Instance.Create(type, name);
}
