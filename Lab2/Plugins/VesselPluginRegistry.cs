using System;
using System.Collections.Generic;
using System.Linq;
using Lab2.Models;

namespace Lab2.Plugins;

public sealed class VesselTypeInfo
{
    public required string TypeKey { get; init; }
    public required string DisplayName { get; init; }
    public string? SourceModule { get; init; }
}

internal sealed class VesselRegistration
{
    public required string TypeKey { get; init; }
    public required string DisplayName { get; init; }
    public required string TypeName { get; init; }
    public required Func<string, SpaceVessel> Factory { get; init; }
    public Action<SpaceVessel, VesselMemento>? CaptureExtra { get; init; }
    public Action<SpaceVessel, VesselMemento>? ApplyExtra { get; init; }
    public IReadOnlyList<VesselUiAction> Actions { get; init; } = Array.Empty<VesselUiAction>();
    public string? SourceModule { get; init; }
    public bool IsPlugin { get; init; }
}

public sealed class VesselPluginRegistry : IVesselRegistry
{
    public static VesselPluginRegistry Instance { get; } = new();

    private readonly Dictionary<string, VesselRegistration> _byTypeKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, VesselRegistration> _byTypeName = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _loadedModules = new();
    private bool _builtInRegistered;

    public event Action? Changed;

    public IReadOnlyList<string> LoadedModules => _loadedModules;

    public void RegisterBuiltInTypes()
    {
        if (_builtInRegistered)
            return;

        _builtInRegistered = true;

        RegisterVessel(
            "Cargo",
            "Cargo",
            typeof(CargoFreighter),
            name => new CargoFreighter(name));

        RegisterVessel(
            "Destroyer",
            "Destroyer",
            typeof(Destroyer),
            name => new Destroyer(name),
            actions: new VesselUiAction
            {
                Label = "Запустить ядерную ракету",
                CanExecute = v => v is Destroyer,
                Execute = v => ((Destroyer)v).LaunchNuke()
            });

        RegisterVessel(
            "Scout",
            "Scout Ship",
            typeof(ScoutFighter),
            name => new ScoutFighter(name));
    }

    public void RegisterVessel(
        string typeKey,
        string displayName,
        Type vesselType,
        Func<string, SpaceVessel> factory,
        Action<SpaceVessel, VesselMemento>? captureExtra = null,
        Action<SpaceVessel, VesselMemento>? applyExtra = null,
        params VesselUiAction[] actions)
    {
        RegisterInternal(new VesselRegistration
        {
            TypeKey = typeKey,
            DisplayName = displayName,
            TypeName = vesselType.Name,
            Factory = factory,
            CaptureExtra = captureExtra,
            ApplyExtra = applyExtra,
            Actions = actions,
            IsPlugin = false
        });
    }

    public void RegisterPluginVessel(
        string sourceModule,
        string typeKey,
        string displayName,
        Type vesselType,
        Func<string, SpaceVessel> factory,
        Action<SpaceVessel, VesselMemento>? captureExtra = null,
        Action<SpaceVessel, VesselMemento>? applyExtra = null,
        params VesselUiAction[] actions)
    {
        RegisterInternal(new VesselRegistration
        {
            TypeKey = typeKey,
            DisplayName = displayName,
            TypeName = vesselType.Name,
            Factory = factory,
            CaptureExtra = captureExtra,
            ApplyExtra = applyExtra,
            Actions = actions,
            SourceModule = sourceModule,
            IsPlugin = true
        });
    }

    private void RegisterInternal(VesselRegistration registration)
    {
        _byTypeKey[registration.TypeKey] = registration;
        _byTypeName[registration.TypeName] = registration;
        Changed?.Invoke();
    }

    public void ClearPluginRegistrations()
    {
        var pluginKeys = _byTypeKey.Values
            .Where(r => r.IsPlugin)
            .Select(r => r.TypeKey)
            .ToList();

        foreach (var key in pluginKeys)
        {
            var reg = _byTypeKey[key];
            _byTypeKey.Remove(key);
            _byTypeName.Remove(reg.TypeName);
        }

        _loadedModules.Clear();
        Changed?.Invoke();
    }

    public void AddLoadedModule(string moduleName)
    {
        if (!_loadedModules.Contains(moduleName))
            _loadedModules.Add(moduleName);
    }

    public IReadOnlyList<VesselTypeInfo> GetAvailableTypes() =>
        _byTypeKey.Values
            .OrderBy(r => r.IsPlugin)
            .ThenBy(r => r.DisplayName)
            .Select(r => new VesselTypeInfo
            {
                TypeKey = r.TypeKey,
                DisplayName = r.DisplayName,
                SourceModule = r.SourceModule
            })
            .ToList();

    public SpaceVessel Create(string typeKey, string name)
    {
        if (!_byTypeKey.TryGetValue(typeKey, out var registration))
            throw new ArgumentException($"Тип корабля '{typeKey}' не зарегистрирован.");

        return registration.Factory(name);
    }

    public SpaceVessel Restore(VesselMemento memento)
    {
        if (!_byTypeName.TryGetValue(memento.TypeName, out var registration))
            throw new FormatException($"Неизвестный тип корабля: '{memento.TypeName}'.");

        var vessel = registration.Factory(memento.ModelName);
        vessel.ApplyDeserializedIdentity(memento.Uid, memento.ModelName, memento.Fuel);

        switch (vessel)
        {
            case CombatShip combat when memento.FirePower.HasValue:
                combat.FirePower = memento.FirePower.Value;
                break;
            case TransportShip transport when memento.Capacity.HasValue:
                transport.Capacity = memento.Capacity.Value;
                break;
        }

        registration.ApplyExtra?.Invoke(vessel, memento);
        return vessel;
    }

    public void CaptureExtra(SpaceVessel vessel, VesselMemento memento)
    {
        if (!_byTypeName.TryGetValue(vessel.GetType().Name, out var registration))
            return;

        registration.CaptureExtra?.Invoke(vessel, memento);
    }

    public IReadOnlyList<VesselUiAction> GetActionsFor(SpaceVessel vessel)
    {
        if (!_byTypeName.TryGetValue(vessel.GetType().Name, out var registration))
            return Array.Empty<VesselUiAction>();

        return registration.Actions
            .Where(a => a.CanExecute(vessel))
            .ToList();
    }
}
