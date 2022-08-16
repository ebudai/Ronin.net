using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

[DebuggerDisplay("{ToString()}")]
public class Expression : Syntax
{
    public List<Syntax> Syntax { get; } = new();

    public bool IsEmpty => Syntax.Count is 0;
    public bool IsScopeClose { get; set; }

    [ExcludeFromCodeCoverage]
    public override string ToString() => string.Join(' ', Syntax);
}
