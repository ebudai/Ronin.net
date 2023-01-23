using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Value : Syntax, Compiler.IParsable<Value>
{
    public static Value Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Reference.Parse(ref parser) ?? Temporary.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Value { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Reference(Value value) => value.value as Reference;
    public static implicit operator Temporary(Value value) => value.value as Temporary;

    private Syntax value;
}
