using System.Globalization;
using Lab2.Models;
using Lab2.Plugins;

namespace Lab2.Plugin.Cargo2;

public sealed class Cargo2Module : IVesselModule
{
    public string ModuleName => "Cargo Mk.II Module";

    public void Register(IVesselRegistry registry)
    {
        registry.RegisterVessel(
            typeKey: "Cargo2",
            displayName: "Cargo Mk.II",
            vesselType: typeof(Cargo2),
            factory: name => new Cargo2(name),
            captureExtra: (vessel, memento) =>
            {
                if (vessel is Cargo2 cargo)
                    memento.SetExtended("ExtraSlots", cargo.ExtraSlots.ToString(CultureInfo.InvariantCulture));
            },
            applyExtra: (vessel, memento) =>
            {
                if (vessel is not Cargo2 cargo)
                    return;

                if (memento.TryGetExtended("ExtraSlots", out var value))
                    cargo.ExtraSlots = int.Parse(value, CultureInfo.InvariantCulture);
            },
            actions: new VesselUiAction
            {
                Label = "Расширить трюм",
                CanExecute = vessel => vessel is Cargo2,
                Execute = vessel => ((Cargo2)vessel).ExpandHold()
            });
    }
}
