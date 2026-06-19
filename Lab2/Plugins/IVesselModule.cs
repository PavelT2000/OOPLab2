namespace Lab2.Plugins;

public interface IVesselModule
{
    string ModuleName { get; }
    void Register(IVesselRegistry registry);
}
