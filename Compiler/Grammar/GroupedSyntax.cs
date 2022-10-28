using Ronin.Compiler;
using System.Collections;

namespace Ronin.Grammar;

internal abstract class GroupedSyntax<T, TOpen, TElement, TSeparator, TClose> : Syntax, IParsable, IEnumerable<TElement>
    where TElement : IParsable 
    where T : GroupedSyntax<T, TOpen, TElement, TSeparator, TClose>, new()
{
    internal TElement this[int index] => _elements[index];    

    public static Syntax Parse(Parser context)
    {
        Parser parser = new(context);
        List<TElement> elements = new();

        if (parser[0] is not TOpen) return null;

        ++parser.Cursor;

        while (parser.IsNotEmpty)
        {
            var syntax = TElement.Parse(parser);
            if (syntax is Error or null) return syntax;
            if (syntax is not TElement element) return Error.Parse(parser);
            elements.Add(element);
            if (parser[0] is TClose)
            {
                ++parser.Cursor;
                break;
            }
            if (parser[0] is TSeparator)
            {
                ++parser.Cursor;
                continue;
            }
            return Error.Parse(parser);
        }

        context.Cursor = parser.Cursor;

        return new T { Tokens = context[..parser.Cursor], _elements = elements.ToArray() };
    }

    public IEnumerator<TElement> GetEnumerator() => _elements.Cast<TElement>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

    private TElement[] _elements;
}
