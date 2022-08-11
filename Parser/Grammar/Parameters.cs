using System.Diagnostics.CodeAnalysis;

namespace Ronin.Parser.Grammar;

internal class Parameters : Syntax
{
    private List<Identifier> Variables { get; } = new();

    internal new static Parameters Parse(Context context)
    {
        context.AddBookmark();

        if (Symbol.Parse(context) is not OpeningParenthesis)
        {
            context.RetreatToLastBookmark();
            return null;
        }

        Parameters parameters = new();
        var syntax = Syntax.Parse(context);
        while (parameters.TryAdd(syntax, context))
        {
            if (syntax is Terminal or Literal)
            {
                context.RetreatToLastBookmark();
                return null;
            }
            syntax = Syntax.Parse(context);
        }

        context.RemoveBookmark();

        return parameters;
    }

    internal bool TryAdd(Syntax syntax, Context context)
    {
        if (syntax is ClosingParenthesis) return false;

        if (syntax is Identifier identifier)
        {
            if (Variables.Count is not 0)
            {
                return Variables[^1].TryAdd(identifier, context);
            }
            Variables.Add(identifier);
        }
        else if (syntax is Separator)
        {
            Variables.Add(new());
        }
        else if (syntax is not Symbol)
        {
            if (Variables.Count is 0) Variables.Add(new());
            Variables[^1].TryAdd(syntax, context);
        }
        return true;
    }

    [ExcludeFromCodeCoverage]
    public override string ToString() => "(" + string.Join(',', Variables) + ")";
}
