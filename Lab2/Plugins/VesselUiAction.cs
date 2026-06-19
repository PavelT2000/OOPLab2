using System;
using Lab2.Models;

namespace Lab2.Plugins;

public sealed class VesselUiAction
{
    public required string Label { get; init; }
    public required Func<SpaceVessel, bool> CanExecute { get; init; }
    public required Action<SpaceVessel> Execute { get; init; }
}
