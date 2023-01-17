using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

internal class Value : Syntax, IParsable
{
    public Syntax Syntax { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Scalar.Parse(ref parser)
            ?? Aggregates.Object.Parse(ref parser)
            ?? Scope.Parse(ref parser)
            ?? Name.Parse(ref parser);

        if (syntax is Error or null) return syntax;

        return new Value { Syntax = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Scalar(Value value) => value.Syntax as Scalar;
    public static implicit operator Aggregates.Object(Value value) => value.Syntax as Aggregates.Object;
    public static implicit operator Scope(Value value) => value.Syntax as Scope;
    public static implicit operator Name(Value value) => value.Syntax as Name;
}
