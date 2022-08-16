using Ronin.Grammar;

namespace Ronin.Parser;

internal static class AggregateParser
{
    internal static Aggregate Parse(Context context)
    {
        context.AddBookmark();

        var startSymbol = SymbolParser.Parse(context);

        var endSymbol = startSymbol switch
        {
            OpeningBrace => new ClosingBrace(),
            OpeningParenthesis => new ClosingParenthesis(),
            OpeningSquareBracket => new ClosingSquareBracket(),
            _ => null as Symbol
        };
        if (endSymbol is null)
        {
            context.RetreatToLastBookmark();
            return null;
        }

        Aggregate aggregate = new();
        Expression expression = new();

        var element = Parser.Parse(context);
        while (element is not Symbol symbol || symbol.Value != endSymbol.Value)
        {
            if (element is Terminal)
            {
                context.RetreatToLastBookmark();
                return null;
            }

            if (element is Separator)
            {
                aggregate.Expressions.Add(expression);
                expression = new();
            }
            else if (!expression.TryAdd(element, context))
            {
                break;
            }

            element = Parser.Parse(context);
        }

        if (!expression.IsEmpty) aggregate.Expressions.Add(expression);

        context.RemoveBookmark();

        return aggregate;
    }
}
