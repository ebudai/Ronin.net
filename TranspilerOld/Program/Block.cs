namespace Ronin.Transpiler.Program;

internal class Block
{
    public Block Parent { get; init; }
    public string Name { get; set; } = string.Empty;
    public Block ReturnType { get; init; }

    public Dictionary<string, Datum> Data { get; init; } = new();
    public Dictionary<string, Block> ImportedPackages { get; init; } = new();    
    public Dictionary<string, Block> Functions { get; init; } = new();
    public Dictionary<string, Block> Types { get; init; } = new();

    public List<Modifier> Modifiers { get; init; } = new();

    public List<Statement> Statements { get; init; } = new();

    public static readonly Block Global = new();
}