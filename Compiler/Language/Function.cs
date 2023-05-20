using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Function : Semantics
{
    public Datatype Returns { get; init; }
    public List<Instruction> Instructions { get; init; } = new();

    public static Function Declare(Grammar.Function function)
    {
        throw new NotImplementedException();
    }
}