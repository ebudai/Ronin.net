using System.Diagnostics;

namespace Ronin.Parser;

[DebuggerDisplay("{ToString()}")]
internal class Expression : Syntax
{
    internal List<Syntax> Syntax { get; } = new();

    internal void Add(Syntax syntax)
    {
        if (Syntax.Count is 0 || Syntax[^1] is not Identifier identifier)
        {
            Syntax.Add(syntax);
            return;
        }
        identifier.Add(syntax);
    }

    public override string ToString() => string.Join(" ", Syntax);
}
