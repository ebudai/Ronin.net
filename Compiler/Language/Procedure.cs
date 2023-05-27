namespace Ronin.Language;

internal class Procedure : Semantics
{
    public List<Instruction> Instructions { get; init; } = new();
}
