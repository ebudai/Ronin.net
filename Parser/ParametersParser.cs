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
            if (@this.Data.Count is not 0)
            {
                return @this.Data[^1].TryAdd(identifier, context);
            }
            @this.Data.Add(identifier);
        }
        else if (syntax is Separator)
        {
            @this.Data.Add(new());
        }
        else if (syntax is not Symbol)
        {
            if (@this.Data.Count is 0) @this.Data.Add(new());
            @this.Data[^1].TryAdd(syntax, context);
        }
        return true;
    }
}
