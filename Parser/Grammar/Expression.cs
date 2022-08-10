using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Parser.Grammar;

[DebuggerDisplay("{ToString()}")]
internal class Expression : Syntax
{
    private List<Syntax> Syntax { get; } = new();

    internal bool IsEmpty => Syntax.Count is 0;
    internal bool IsScopeClose { get; private set; }

    internal bool TryAdd(Declaration declaration, ref int cursor)
    {
        if (Syntax.Count is 0 || Syntax[^1] is not Scope)
        {
            return TryAdd(declaration as Identifier, ref cursor);
        }
        cursor -= declaration.ToString().Length;
        return false;        
    }

    internal bool TryAdd(Identifier identifier, ref int cursor)
    {
        if (Syntax.Count is 0 || Syntax[^1] is not Identifier prioridentifier)
        {
            Syntax.Add(identifier);
            return true;
        }

        return prioridentifier.TryAdd(identifier, ref cursor);        
    }

    internal bool TryAdd(Syntax syntax, ref int cursor)
    {
        IsScopeClose = syntax is ClosingBrace;

        return syntax switch
        {
            null => false,
            Declaration declaration => TryAdd(declaration, ref cursor),
            Identifier identifier => TryAdd(identifier, ref cursor),
            Symbol => false,
            _ => Add(syntax)
        };
        
        bool Add(Syntax syntax)
        {
            Syntax.Add(syntax);
            return true;
        }
    }

    [ExcludeFromCodeCoverage]
    public override string ToString() => string.Join(' ', Syntax);
}
