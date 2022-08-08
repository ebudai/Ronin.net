using System.Diagnostics;

namespace Ronin.Parser.Grammar;

[DebuggerDisplay("{ToString()}")]
internal class Expression : Syntax
{
    private List<Syntax> Syntax { get; } = new();

    internal bool IsEmpty => Syntax.Count is 0;
    internal bool IsScopeClose { get; private set; }

    internal bool TryAdd(Declaration declaration, ref int cursor)
    { 
        if (Syntax.Count > 0 && Syntax[^1] is Scope)
        {
            cursor -= declaration.ToString().Length;
            return false;
        }
        return TryAdd(declaration as Identifier, ref cursor);
    }

    internal bool TryAdd(Identifier identifier, ref int cursor)
    {
        if (Syntax.Count > 0 && Syntax[^1] is Identifier prioridentifier)
        {
            return prioridentifier.TryAdd(identifier, ref cursor);
        }
        Syntax.Add(identifier);
        return true;
    }

    internal bool TryAdd(Syntax syntax, ref int cursor)
    {
        if (syntax is null) return false;

        if (syntax is Declaration declaration) return TryAdd(declaration, ref cursor);
        if (syntax is Identifier identifier) return TryAdd(identifier, ref cursor);

        IsScopeClose = syntax is ClosingBrace;
        if (syntax is Symbol) return false;
        
        Syntax.Add(syntax);
        return true;
    }

    public override string ToString() => string.Join(' ', Syntax);
}
