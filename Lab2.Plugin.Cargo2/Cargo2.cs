using Lab2.Models;

namespace Lab2.Plugin.Cargo2;

public sealed class Cargo2 : TransportShip
{
    private readonly string[] _cargoLog = new string[10];

    [ShowInInspector("Доп. отсеки")]
    public int ExtraSlots { get; set; }

    public Cargo2(string name) : base(name, 3500)
    {
        ExtraSlots = 2;
    }

    public string this[int index]
    {
        get => _cargoLog[index];
        set => _cargoLog[index] = value;
    }

    public void ExpandHold()
    {
        ExtraSlots += 1;
        Capacity += 500;
    }

    public override void ExecuteMission()
    {
        base.ExecuteMission();
        if (ExtraSlots > 0)
            ExtraSlots -= 1;
    }

    public override string GetStatus()
    {
        return base.GetStatus() + $" | Доп. отсеки: {ExtraSlots}";
    }
}
