using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Lab2.Factories;
using Lab2.Models;
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
    public ObservableCollection<SpaceVessel> CreatedShips { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();
        ShipsList.ItemsSource = CreatedShips;
        DataContext = this;
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
                new("˜˜˜˜˜˜˜˜˜ ˜˜˜˜ (*.txt)") { Patterns = new[] { "*.txt" } },
                FilePickerFileTypes.All
            },
            1 => new List<FilePickerFileType>
            {
                new("JSON (*.json)") { Patterns = new[] { "*.json" } },
                FilePickerFileTypes.All
            },
            2 => new List<FilePickerFileType>
            {
                new("˜˜˜˜˜˜˜˜ (*.bin)") { Patterns = new[] { "*.bin" } },
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
            Title = "˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜",
            Content = panel,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        ok.Click += (_, _) => win.Close();
        await win.ShowDialog(owner);
    }

    private async void SaveFleet_Click(object? sender, RoutedEventArgs e)
    {
        var formatIndex = SerializerTypeComboBox.SelectedIndex;
        var serializer = GetSerializer();
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "˜˜˜˜˜˜˜˜˜ ˜˜˜˜",
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
            Title = "˜˜˜˜˜˜˜˜˜ ˜˜˜˜",
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
        var selectedType = (TypeSelector.SelectedItem as ComboBoxItem)?.Content?.ToString();

        SpaceVessel newShip = selectedType switch
        {
            "Cargo" => VesselFactory.CreateVessel("Cargo", "Cargo"),
            "Destroyer" => VesselFactory.CreateVessel("Destroyer", "Destroyer"),
            "Scout Ship" => VesselFactory.CreateVessel("Scout", "Scout"),
            _ => throw new InvalidOperationException("˜˜˜˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜˜˜.")
        };

        CreatedShips.Add(newShip);
        ShipsList.SelectedItem = newShip;
    }

    private void ShipsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateInspector(ShipsList.SelectedItem);
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
            var chk = new CheckBox { Content = "˜˜/˜˜˜" };
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
