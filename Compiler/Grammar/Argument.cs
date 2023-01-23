using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Argument : Syntax, Compiler.IParsable<Argument>
{
    public static Argument Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Reference.Parse(ref parser) ?? Value.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Argument { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Reference(Argument argument) => argument.value as Reference;
    public static implicit operator Value(Argument argument) => argument.value as Value;

    private Syntax value;
}
