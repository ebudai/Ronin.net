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
            for (var i = 0; i != identifier.ComponentCount; ++i)
            {
                if (identifier.Names.TryGetValue(i, out var name)) @this.Add(name);
                else if (identifier.Parameters.TryGetValue(i, out var paramter)) @this.Add(paramter);
            }
        }
        else if (syntax is Parameters parameters)
        {
            //TODO is this really needed?  code coverage says it is never used
            @this.Parameters.Add(@this.Names.Count, parameters);
        }
        else
        {
            if (!@this.Parameters.TryGetValue(@this.Names.Count, out parameters))
            {
                parameters = new();
                @this.Parameters.Add(@this.Names.Count, parameters);
            }
            return parameters.TryAdd(syntax, context);
        }
        return true;
    }
}
