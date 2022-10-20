using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Reference : Syntax, IParsable
{
    internal SortedList<int, Name> Names { get; private init; }
    internal SortedList<int, Value> Arguments { get; private init; }

    private Reference(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        SortedList<int, Name> names = new();
        SortedList<int, Value> arguments = new();
        Parser attempt = new(parser);
        while (attempt.IsNotEmpty)
        {
            var syntax = Name.Parse(attempt);
            if (syntax is Name name)
            {
                names.Add(names.Count + arguments.Count, name);
            }
            else
            {
                syntax = Value.Parse(attempt);
                if (syntax is Scalar scalar) arguments.Add(names.Count + arguments.Count, scalar);
                else if (syntax is Aggregate aggregate) arguments.Add(names.Count + arguments.Count, aggregate);
                else if (syntax is Unexpected unexpected) return unexpected;
                else if (syntax is null) break;
            }
        }

        return names.Count is 0 && arguments.Count is 0 ? null : new Reference(parser, attempt.Cursor) { Names = names, Arguments = arguments };
    }
}