using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Parser.Grammar;

[DebuggerDisplay("{ToString()}")]
internal class Expression : Syntax
{
    private List<Syntax> Syntax { get; } = new();

    internal bool IsEmpty => Syntax.Count is 0;
    internal bool IsScopeClose { get; private set; }

    internal new static Expression Parse(Context context)
    {
        Expression expression = new();

        while (expression.TryAdd(Ronin.Parser.Syntax.Parse(context), context)) { }

        return expression.IsEmpty ? null : expression;
    }

    internal bool TryAdd(Declaration declaration, Context parser)
    {
        if (Syntax.Count is 0 || Syntax[^1] is not Scope)
        {
            return TryAdd(declaration as Identifier, parser);
        }
        parser.Retreat(declaration.ToString().Length);
        return false;        
    }

    internal bool TryAdd(Identifier identifier, Context context)
    {
        if (Syntax.Count is 0 || Syntax[^1] is not Identifier prioridentifier)
        {
            Syntax.Add(identifier);
            return true;
        }

        return prioridentifier.TryAdd(identifier, context);
    }

    internal bool TryAdd(Syntax syntax, Context context)
    {
        IsScopeClose = syntax is ClosingBrace;

        return syntax switch
        {
            null => false,
            Declaration declaration => TryAdd(declaration, context),
            Identifier identifier => TryAdd(identifier, context),
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
