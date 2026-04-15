using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Lab2.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Lab2.Factories;

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

    private void CreateShip_Click(object? sender, RoutedEventArgs e)
    {
        var selectedType = (TypeSelector.SelectedItem as ComboBoxItem)?.Content?.ToString();

        SpaceVessel newShip = selectedType switch
        {
            "Cargo" => VesselFactory.CreateVessel("Cargo","Cargo"),
            "Destroyer" => VesselFactory.CreateVessel("Destroyer", "Destroyer"),
            "Scout Ship" => VesselFactory.CreateVessel("Scout", "Scout"),
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

        // Получаем ВСЕ публичные свойства
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Сортируем для красоты: база вверху, остальное ниже
        var sorted = properties.OrderBy(p => p.DeclaringType == typeof(SpaceVessel) ? 0 : 1);

        foreach (var prop in sorted)
        {
            // Пытаемся взять красивое имя из атрибута, если его нет — берем системное имя поля
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

        // Если тип - число
        if (prop.PropertyType == typeof(int))
        {
            var num = new NumericUpDown();
            num.Bind(NumericUpDown.ValueProperty, binding);
            input = num;
        }
        // Если тип - логический
        else if (prop.PropertyType == typeof(bool))
        {
            var chk = new CheckBox { Content = "Да/Нет" };
            chk.Bind(CheckBox.IsCheckedProperty, binding);
            input = chk;
        }
        // Всё остальное в текст
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