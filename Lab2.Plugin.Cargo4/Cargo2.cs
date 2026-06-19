using Lab2.Models;

namespace Lab2.Plugin.Cargo2;

public sealed class Cargo2 : TransportShip
{
    private readonly string[] _cargoLog = new string[10];

    [ShowInInspector("Мед. отсеки")]
    public int MedSlots { get; set; }

    public Cargo2(string name) : base(name, 3500)
    {
        MedSlots = 2;
    }

    public string this[int index]
    {
        get => _cargoLog[index];
        set => _cargoLog[index] = value;
    }

    public void ExpandHold()
    {
        MedSlots += 1;
        Capacity += 500;
    }

    public override void ExecuteMission()
    {
        base.ExecuteMission();
        if (MedSlots > 0)
            MedSlots -= 1;
    }

    public override string GetStatus()
    {
        return base.GetStatus() + $" | Доп. отсеки: {MedSlots}";
    }
}
