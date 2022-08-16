using Ronin.Grammar;

namespace Ronin.Parser;

internal static class ParametersParser
{
    internal static Parameters Parse(Context context)
    {
        context.AddBookmark();

        if (SymbolParser.Parse(context) is not OpeningParenthesis)
        {
            context.RetreatToLastBookmark();
            return null;
        }

        Parameters parameters = new();
        var syntax = Parser.Parse(context);
        while (parameters.TryAdd(syntax, context))
        {
            if (syntax is Terminal or Literal)
            {
                context.RetreatToLastBookmark();
                return null;
            }
            syntax = Parser.Parse(context);
        }

        context.RemoveBookmark();

        return parameters;
    }

    internal static bool TryAdd(this Parameters @this, Syntax syntax, Context context)
    {
        if (syntax is ClosingParenthesis) return false;

        if (syntax is Identifier identifier)
        {
            if (@this.Variables.Count is not 0)
            {
                return @this.Variables[^1].TryAdd(identifier, context);
            }
            @this.Variables.Add(identifier);
        }
        else if (syntax is Separator)
        {
            @this.Variables.Add(new());
        }
        else if (syntax is not Symbol)
        {
            if (@this.Variables.Count is 0) @this.Variables.Add(new());
            @this.Variables[^1].TryAdd(syntax, context);
        }
        return true;
    }
}
