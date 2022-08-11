namespace Ronin.Parser.Grammar.Aggregates;

internal class Aggregate : Syntax
{
    protected internal List<Expression> Expressions { get; } = new();

    internal static T Parse<T>(string open, string close, Context context) where T : Aggregate
    {
        context.AddBookmark();

        if (Symbol.Parse(context)?.Value != open)
        {
            context.RetreatToLastBookmark();
            return null;
        }

        var aggregate = Activator.CreateInstance<T>();
        Expression expression = new();
        
        var element = Parse(context);
        while (element is not Symbol symbol || symbol.Value != close)
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

            element = Parse(context);
        }

        if (!expression.IsEmpty) aggregate.Expressions.Add(expression);

        context.RemoveBookmarks();

        return aggregate;
    }
}
