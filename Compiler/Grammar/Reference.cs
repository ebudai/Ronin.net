using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Reference : Syntax, IParsable
{
    internal SortedList<int, Name> Names { get; private init; }
    internal SortedList<int, Value> Arguments { get; private init; }

    public static Syntax Parse(Parser context)
    {
        SortedList<int, Name> names = new();
        SortedList<int, Value> arguments = new();
        Parser parser = new(context);
        while (parser.IsNotEmpty)
        {
            var syntax = Name.Parse(parser);
            if (syntax is Name name)
            {
                names.Add(names.Count + arguments.Count, name);
            }
            else
            {
                syntax = Value.Parse(parser);
                if (syntax is Scalar scalar) arguments.Add(names.Count + arguments.Count, scalar);
                else if (syntax is Aggregate aggregate) arguments.Add(names.Count + arguments.Count, aggregate);
                else if (syntax is Error) return syntax;
                else if (syntax is null) break;
            }
        }

        if (names.Count is 0 && arguments.Count is 0) return null;

        return new Reference { Names = names, Arguments = arguments, Tokens = context[..parser.Cursor] };
    }
}