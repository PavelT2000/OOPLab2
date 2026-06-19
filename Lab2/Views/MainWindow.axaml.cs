using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Lab2.Factories;
using Lab2.Models;
using Lab2.Plugins;
using Lab2.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Lab2.Views;

public partial class MainWindow : Window
{
    private readonly VesselModuleLoader _moduleLoader = new();
    private bool _modulesLoadedOnce;

    public ObservableCollection<SpaceVessel> CreatedShips { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();
        ShipsList.ItemsSource = CreatedShips;
        DataContext = this;

        VesselPluginRegistry.Instance.RegisterBuiltInTypes();
        LoadModules(reload: false);
        VesselPluginRegistry.Instance.Changed += RefreshVesselTypes;
    }

    private IFleetSerializer GetSerializer() => SerializerTypeComboBox.SelectedIndex switch
    {
        0 => new TextFleetSerializer(),
        1 => new JsonFleetSerializer(),
        2 => new BinaryFleetSerializer(),
        _ => new TextFleetSerializer()
    };

    private static string SuggestedFileName(int formatIndex) => formatIndex switch
    {
        0 => "fleet.txt",
        1 => "fleet.json",
        2 => "fleet.bin",
        _ => "fleet.txt"
    };

    private static string DefaultExtension(int formatIndex) => formatIndex switch
    {
        0 => "txt",
        1 => "json",
        2 => "bin",
        _ => "txt"
    };

    private static List<FilePickerFileType> FilePickerTypes(int formatIndex)
    {
        return formatIndex switch
        {
            0 => new List<FilePickerFileType>
            {
                new("Текстовый (*.txt)") { Patterns = new[] { "*.txt" } },
                FilePickerFileTypes.All
            },
            1 => new List<FilePickerFileType>
            {
                new("JSON (*.json)") { Patterns = new[] { "*.json" } },
                FilePickerFileTypes.All
            },
            2 => new List<FilePickerFileType>
            {
                new("Бинарный (*.bin)") { Patterns = new[] { "*.bin" } },
                FilePickerFileTypes.All
            },
            _ => new List<FilePickerFileType> { FilePickerFileTypes.All }
        };
    }

    private static async Task ShowErrorDialogAsync(Window owner, string message)
    {
        var ok = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 88,
            Margin = new Avalonia.Thickness(0, 12, 0, 0)
        };
        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 440
        });
        panel.Children.Add(ok);
        var win = new Window
        {
            Title = "Ошибка",
            Content = panel,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        ok.Click += (_, _) => win.Close();
        await win.ShowDialog(owner);
    }

    private static async Task ShowInfoDialogAsync(Window owner, string title, string message)
    {
        var ok = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 88,
            Margin = new Avalonia.Thickness(0, 12, 0, 0)
        };
        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 440
        });
        panel.Children.Add(ok);
        var win = new Window
        {
            Title = title,
            Content = panel,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        ok.Click += (_, _) => win.Close();
        await win.ShowDialog(owner);
    }

    private void LoadModules(bool reload)
    {
        var result = _moduleLoader.LoadAll(reload);
        _modulesLoadedOnce = true;
        RefreshVesselTypes();
        UpdateModulesStatus(result);
    }

    private void UpdateModulesStatus(ModuleLoadResult result)
    {
        var lines = new List<string>
        {
            $"Папка: {_moduleLoader.PluginsDirectory}"
        };

        if (result.Loaded.Count > 0)
            lines.Add("Загружено: " + string.Join(", ", result.Loaded));
        else
            lines.Add("Дополнительные модули не загружены.");

        if (result.Errors.Count > 0)
            lines.Add("Ошибки: " + string.Join("; ", result.Errors));

        ModulesStatusText.Text = string.Join(Environment.NewLine, lines);
    }

    private void RefreshVesselTypes()
    {
        var selectedKey = (TypeSelector.SelectedItem as ComboBoxItem)?.Tag as string;
        TypeSelector.Items.Clear();

        foreach (var type in VesselPluginRegistry.Instance.GetAvailableTypes())
        {
            var label = type.SourceModule is null
                ? type.DisplayName
                : $"{type.DisplayName} [{type.SourceModule}]";

            TypeSelector.Items.Add(new ComboBoxItem
            {
                Content = label,
                Tag = type.TypeKey
            });
        }

        if (TypeSelector.Items.Count == 0)
            return;

        var index = 0;
        if (selectedKey is not null)
        {
            for (var i = 0; i < TypeSelector.Items.Count; i++)
            {
                if ((TypeSelector.Items[i] as ComboBoxItem)?.Tag as string == selectedKey)
                {
                    index = i;
                    break;
                }
            }
        }

        TypeSelector.SelectedIndex = index;
    }

    private async void ReloadModules_Click(object? sender, RoutedEventArgs e)
    {
        LoadModules(reload: _modulesLoadedOnce);

        var pluginTypes = VesselPluginRegistry.Instance.GetAvailableTypes()
            .Where(t => t.SourceModule is not null)
            .Select(t => t.DisplayName)
            .ToList();

        var message = pluginTypes.Count > 0
            ? "Модули перезагружены. Доступные типы из плагинов:\n" + string.Join("\n", pluginTypes)
            : "Модули перезагружены. Новые типы кораблей не обнаружены — положите DLL в папку Plugins и пересоберите модуль.";

        await ShowInfoDialogAsync(this, "Модули", message);
    }

    private async void SaveFleet_Click(object? sender, RoutedEventArgs e)
    {
        var formatIndex = SerializerTypeComboBox.SelectedIndex;
        var serializer = GetSerializer();
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить флот",
            SuggestedFileName = SuggestedFileName(formatIndex),
            DefaultExtension = DefaultExtension(formatIndex),
            FileTypeChoices = FilePickerTypes(formatIndex)
        });

        if (file is null)
            return;

        await using var stream = await file.OpenWriteAsync();
        serializer.Serialize(stream, CreatedShips);
    }

    private async void LoadFleet_Click(object? sender, RoutedEventArgs e)
    {
        var formatIndex = SerializerTypeComboBox.SelectedIndex;
        var serializer = GetSerializer();
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Загрузить флот",
            AllowMultiple = false,
            FileTypeFilter = FilePickerTypes(formatIndex)
        });

        var file = files?.Count > 0 ? files[0] : null;
        if (file is null)
            return;

        await using var stream = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        IReadOnlyList<SpaceVessel> loaded;
        try
        {
            loaded = serializer.Deserialize(ms);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(this, ex.Message);
            return;
        }

        foreach (var vessel in loaded)
            CreatedShips.Add(vessel);

        if (loaded.Count > 0)
            ShipsList.SelectedItem = loaded[^1];
    }

    private void RemoveShip_Click(object? sender, RoutedEventArgs e)
    {
        if (ShipsList.SelectedItem is not SpaceVessel vessel)
            return;

        CreatedShips.Remove(vessel);
        UpdateInspector(ShipsList.SelectedItem);
    }

    private void CreateShip_Click(object? sender, RoutedEventArgs e)
    {
        if (TypeSelector.SelectedItem is not ComboBoxItem item || item.Tag is not string typeKey)
            return;

        var displayName = VesselPluginRegistry.Instance.GetAvailableTypes()
            .FirstOrDefault(t => t.TypeKey == typeKey)?.DisplayName ?? "Ship";

        var newShip = VesselFactory.CreateVessel(typeKey, displayName);
        CreatedShips.Add(newShip);
        ShipsList.SelectedItem = newShip;
    }

    private void ShipsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateInspector(ShipsList.SelectedItem);
        UpdateModuleActions(ShipsList.SelectedItem as SpaceVessel);
    }

    private void UpdateModuleActions(SpaceVessel? vessel)
    {
        ModuleActionsPanel.Children.Clear();
        if (vessel is null)
            return;

        foreach (var action in VesselPluginRegistry.Instance.GetActionsFor(vessel))
        {
            var capturedAction = action;
            var button = new Button
            {
                Content = capturedAction.Label,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = Brushes.LightSkyBlue
            };

            button.Click += (_, _) =>
            {
                if (ShipsList.SelectedItem is not SpaceVessel selected)
                    return;

                capturedAction.Execute(selected);
                UpdateInspector(selected);
            };

            ModuleActionsPanel.Children.Add(button);
        }
    }

    private void UpdateInspector(object? obj)
    {
        InspectorPanel.Children.Clear();
        if (obj == null) return;

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var sorted = properties.OrderBy(p => p.DeclaringType == typeof(SpaceVessel) ? 0 : 1);

        foreach (var prop in sorted)
        {
            if (!prop.CanWrite)
                continue;
            var attr = prop.GetCustomAttribute<ShowInInspectorAttribute>();
            string displayName = attr?.Label ?? prop.Name;

            var control = CreateControlForProperty(obj, prop, displayName);
            InspectorPanel.Children.Add(control);
        }
    }

    private Control CreateControlForProperty(object obj, PropertyInfo prop, string labelName)
    {
        var container = new StackPanel { Margin = new Avalonia.Thickness(0, 0, 0, 5) };
        container.Children.Add(new TextBlock { Text = labelName, FontSize = 12, Foreground = Avalonia.Media.Brushes.Gray });

        Control input;
        var binding = new Binding(prop.Name) { Source = obj, Mode = BindingMode.TwoWay };

        if (prop.PropertyType == typeof(int))
        {
            var num = new NumericUpDown();
            num.Bind(NumericUpDown.ValueProperty, binding);
            input = num;
        }
        else if (prop.PropertyType == typeof(bool))
        {
            var chk = new CheckBox { Content = "??/???" };
            chk.Bind(CheckBox.IsCheckedProperty, binding);
            input = chk;
        }
        else
        {
            var txt = new TextBox();
            txt.Bind(TextBox.TextProperty, binding);
            input = txt;
        }

        container.Children.Add(input);
        return container;
    }

    private void Execute_Click(object? sender, RoutedEventArgs e)
    {
        if (ShipsList.SelectedItem is SpaceVessel vessel)
        {
            vessel.ExecuteMission();
            UpdateInspector(vessel);
        }
    }
}
