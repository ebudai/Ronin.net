namespace Ronin.Program;

internal class Datum
{
    public Block Type { get; init; }

    public readonly List<Modifier> Modifiers = new();
}
