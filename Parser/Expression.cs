using System.Diagnostics;

namespace Ronin.Parser;

[DebuggerDisplay("{ToString()}")]
internal class Expression : Syntax
{
    private List<Syntax> Syntax { get; } = new();

    internal bool IsEmpty => Syntax.Count is 0;

    internal bool Add(Keyword keyword, ref int cursor)
    { 
        if (Syntax.Count > 0 && Syntax[^1] is Scope)
        {
            cursor -= keyword.ToString().Length;
            return false;
        }
        return Add(keyword as Identifier, ref cursor);
    }

    /*internal bool Add(Literal literal, ref int cursor)
    {
        if (Syntax.Count > 0 && Syntax[^1] is Identifier identifier)
        {
            return identifier.Add(literal, ref cursor);
        }
        Syntax.Add(literal);
        cursor += identifier.ToString().Length;
        return true;
    }*/

    internal bool Add(Identifier identifier, ref int cursor)
    {
        if (Syntax.Count > 0 && Syntax[^1] is Identifier prioridentifier)
        {
            return prioridentifier.Add(identifier, ref cursor);
        }
        Syntax.Add(identifier);
        cursor += identifier.ToString().Length;
        return true;
    }

    internal bool Add(Syntax syntax, ref int cursor)
    {
        if (syntax is Keyword keyword) return Add(keyword, ref cursor);
        //if (syntax is Literal literal) return Add(literal, ref cursor);
        if (syntax is Identifier identifier) return Add(identifier, ref cursor);
        if (syntax is ClosingBrace brace) cursor -= brace.Value.Length;
        if (syntax is Symbol) return false;
        Syntax.Add(syntax);
        return true;
    }

    public override string ToString() => string.Join(" ", Syntax);
}
