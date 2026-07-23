namespace ISMA.Domain.Models;

public sealed class BlueprintStateModel
{
    public Guid Id { get; init; } = Guid.Empty;
    public double CanvasPositionX { get; set; }
    public double CanvasPositionY { get; set; }
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";

    public BlueprintStateModel() { }

    public BlueprintStateModel(double x, double y, string name, string text)
    {
        Id = Guid.NewGuid();
        CanvasPositionX = x;
        CanvasPositionY = y;
        Name = name;
        Text = text;
    }
}
