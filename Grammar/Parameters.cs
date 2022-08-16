using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

public class Parameters : Syntax
{
    public List<Identifier> Variables { get; } = new();

    [ExcludeFromCodeCoverage]
    public override string ToString() => "(" + string.Join(',', Variables) + ")";
}
