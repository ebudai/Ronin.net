namespace Ronin.Program;

internal class Block
{
    public string Name { get; private init; }
    public Block Parent { get; private init; }
    public Dictionary<string, Block> Imports { get; } = new();
    public Dictionary<string, Datum> Data { get; } = new();
    public Dictionary<string[], Function> Functions { get; } = new();
    public Dictionary<string[], Datatype> Datatypes { get; } = new();

    public static Block Global { get; }

    static Block()
    {
        Global = new();

    }
}
