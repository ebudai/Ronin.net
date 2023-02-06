using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

internal class Value : Syntax, Compiler.IParsable<Value>
{
    public static Value Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Reference.Parse(ref parser)
            ?? Scalar.Parse(ref parser)
            ?? Arguments.Parse(ref parser)
            ?? Scope.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Value { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Reference(Value value) => value.value as Reference;
    public static implicit operator Scalar(Value value) => value.value as Scalar;
    public static implicit operator Arguments(Value value) => value.value as Arguments;
    
    private Syntax value;
}
