using Ronin.Grammar;

namespace Ronin.Parser;

internal static class ExpressionParser
{
    internal static Expression Parse(Context context)
    {
        Expression expression = new();

        while (expression.TryAdd(Parser.Parse(context), context)) { }

        return expression.IsEmpty ? null : expression;
    }

    private static bool TryAdd(this Expression @this, Declaration declaration, Context parser)
    {
        if (@this.Syntax.Count is 0 || @this.Syntax[^1] is not Scope)
        {
            return TryAdd(@this, declaration as Identifier, parser);
        }
        parser.Retreat(declaration.ToString().Length);
        return false;
    }

    internal static bool TryAdd(this Expression @this, Identifier identifier, Context context)
    {
        if (@this.Syntax.Count is 0 || @this.Syntax[^1] is not Identifier prioridentifier)
        {
            @this.Syntax.Add(identifier);
            return true;
        }

        return prioridentifier.TryAdd(identifier, context);
    }

    internal static bool TryAdd(this Expression @this, Syntax syntax, Context context)
    {
        @this.IsScopeClose = syntax is ClosingBrace;

        return syntax switch
        {
            null => false,
            Declaration declaration => @this.TryAdd(declaration, context),
            Identifier identifier => @this.TryAdd(identifier, context),
            Symbol => false,
            _ => Add(syntax)
        };

        bool Add(Syntax syntax)
        {
            @this.Syntax.Add(syntax);
            return true;
        }
    }
}
