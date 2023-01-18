using Ronin.Compiler;

namespace Ronin.Grammar;

internal abstract class Aggregate<T, TOpen, TElement, TSeparator, TClose> : Syntax, IParsable
    where T : Aggregate<T, TOpen, TElement, TSeparator, TClose>, new()
    where TElement : class, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        if (context.Current is not TOpen) return null;

        Parser parser = context;
        List<TElement> values = new();
        parser.Advance();

        while (parser.IsNotFinished)
        {
            var syntax = TElement.Parse(ref parser);
            if (syntax is Error) return syntax;
            if (syntax is null)
            {
                if (parser.Current is not TClose) return null;
                parser.Advance();
                break;
            }
            values.Add(syntax as TElement);
            if (parser.Current is TSeparator) parser.Advance();
        }

        return new T { Values = values, Source = parser.Commit(ref context) };
    }

    protected internal List<TElement> Values;
}