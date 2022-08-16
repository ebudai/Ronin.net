using Ronin.Grammar;

namespace Ronin.Parser;

internal static class IdentifierParser
{
    internal static Identifier Parse(Context context)
    {
        var lexed = context.Lex(Form.identifier);
        return lexed is null ? null : new(lexed);
    }

    internal static bool TryAdd(this Identifier @this, Syntax syntax, Context context)
    {
        if (syntax is Identifier identifier)
        {
            @this.Names.AddRange(identifier.Names);
        }
        else if (syntax is Expression expression)
        {
            //TODO is this really needed?  code coverage says it is never used
            @this.Parameters.Add(@this.Names.Count, expression);
        }
        else
        {
            if (!@this.Parameters.TryGetValue(@this.Names.Count, out expression))
            {
                expression = new();
                @this.Parameters.Add(@this.Names.Count, expression);
            }
            return expression.TryAdd(syntax, context);
        }
        return true;
    }
}
