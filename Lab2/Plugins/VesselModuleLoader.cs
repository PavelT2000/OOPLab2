using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Lab2.Models;

namespace Lab2.Plugins;

public sealed class ModuleLoadResult
{
    public List<string> Loaded { get; } = new();
    public List<string> Errors { get; } = new();
}

internal sealed class PluginAssemblyLoadContext : AssemblyLoadContext
{
    public PluginAssemblyLoadContext(string name) : base(name, isCollectible: true) { }

    protected override Assembly? Load(AssemblyName assemblyName) => null;
}

public sealed class VesselModuleLoader
{
    private readonly string _pluginsDirectory;
    private readonly List<PluginAssemblyLoadContext> _contexts = new();

    public VesselModuleLoader()
    {
        _pluginsDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
        Directory.CreateDirectory(_pluginsDirectory);
    }

    public string PluginsDirectory => _pluginsDirectory;

    public ModuleLoadResult LoadAll(bool reload = false)
    {
        var result = new ModuleLoadResult();

        if (reload)
        {
            VesselPluginRegistry.Instance.ClearPluginRegistrations();
            foreach (var context in _contexts)
                context.Unload();
            _contexts.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        foreach (var dllPath in Directory.GetFiles(_pluginsDirectory, "*.dll").OrderBy(Path.GetFileName))
        {
            try
            {
                var context = new PluginAssemblyLoadContext(Path.GetFileNameWithoutExtension(dllPath));
                _contexts.Add(context);
                var assembly = context.LoadFromAssemblyPath(dllPath);
                LoadModulesFromAssembly(assembly, result);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{Path.GetFileName(dllPath)}: {ex.Message}");
            }
        }

        return result;
    }

    private static void LoadModulesFromAssembly(Assembly assembly, ModuleLoadResult result)
    {
        var moduleTypes = assembly.GetTypes()
            .Where(t => typeof(IVesselModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

        var loadedAny = false;
        foreach (var type in moduleTypes)
        {
            if (Activator.CreateInstance(type) is not IVesselModule module)
                continue;

            var registrar = new PluginVesselRegistrar(module.ModuleName);
            module.Register(registrar);
            VesselPluginRegistry.Instance.AddLoadedModule(module.ModuleName);
            result.Loaded.Add(module.ModuleName);
            loadedAny = true;
        }

        if (!loadedAny)
            result.Errors.Add($"{assembly.GetName().Name}: не найден класс, реализующий IVesselModule.");
    }

    private sealed class PluginVesselRegistrar : IVesselRegistry
    {
        private readonly string _moduleName;

        public PluginVesselRegistrar(string moduleName) => _moduleName = moduleName;

        public void RegisterVessel(
            string typeKey,
            string displayName,
            Type vesselType,
            Func<string, SpaceVessel> factory,
            Action<SpaceVessel, VesselMemento>? captureExtra = null,
            Action<SpaceVessel, VesselMemento>? applyExtra = null,
            params VesselUiAction[] actions)
        {
            VesselPluginRegistry.Instance.RegisterPluginVessel(
                _moduleName,
                typeKey,
                displayName,
                vesselType,
                factory,
                captureExtra,
                applyExtra,
                actions);
        }
    }
}
