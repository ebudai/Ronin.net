namespace Ronin.Program;

internal class Instruction
{
    public Block ReturnType { get; init; }
    public Dictionary<string, Datum> Data { get; } = new();
}

internal class Call : Instruction
{    
    public Dictionary<string, Datum> Args { get; } = new();
}

internal class Scope : Instruction
{
    
}