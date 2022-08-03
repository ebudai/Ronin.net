namespace Ronin.Transpiler.Program;

internal class Datum
{
    public Block Type { get; init; }
    public Statement Initializer { get; init; }

    public readonly List<Modifier> Modifiers = new();
}
