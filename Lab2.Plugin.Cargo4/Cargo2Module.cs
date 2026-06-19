using System.Globalization;
using Lab2.Models;
using Lab2.Plugins;

namespace Lab2.Plugin.Cargo2;

public sealed class Cargo2Module : IVesselModule
{
    public string ModuleName => "Medical ship";

    public void Register(IVesselRegistry registry)
    {
        registry.RegisterVessel(
            typeKey: "Med",
            displayName: "Med",
            vesselType: typeof(Cargo2),
            factory: name => new Cargo2(name),
            captureExtra: (vessel, memento) =>
            {
                if (vessel is Cargo2 cargo)
                    memento.SetExtended("MedSlots", cargo.MedSlots.ToString(CultureInfo.InvariantCulture));
            },
            applyExtra: (vessel, memento) =>
            {
                if (vessel is not Cargo2 cargo)
                    return;

                if (memento.TryGetExtended("MedSlots", out var value))
                    cargo.MedSlots = int.Parse(value, CultureInfo.InvariantCulture);
            },
            actions: new VesselUiAction
            {
                Label = "No name",
                CanExecute = vessel => vessel is Cargo2,
                Execute = vessel => ((Cargo2)vessel).ExpandHold()
            });
    }
}
