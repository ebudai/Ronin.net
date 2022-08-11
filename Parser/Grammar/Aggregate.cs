namespace Ronin.Parser.Grammar;

internal class Aggregate : Syntax
{
    protected internal List<Expression> Expressions { get; } = new();

    internal new static Aggregate Parse(Context context)
    {
        context.AddBookmark();

        var open = Symbol.Parse(context);

        var close = open switch
        {
            OpeningBrace => new ClosingBrace(),
            OpeningParenthesis => new ClosingParenthesis(),
            OpeningSquareBracket => new ClosingSquareBracket(),
            _ => Fail(context)
        };
        if (close is null) return null;

        Aggregate aggregate = new();
        Expression expression = new();
        
        var element = Syntax.Parse(context);
        while (element is not Symbol symbol || symbol.Value != close.Value)
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

            element = Syntax.Parse(context);
        }

        if (!expression.IsEmpty) aggregate.Expressions.Add(expression);

        context.RemoveBookmark();

        return aggregate;

        static Symbol Fail(Context context)
        {
            context.RetreatToLastBookmark();
            return null;
        }
    }
}
