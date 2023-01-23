using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

internal class Argument : Syntax, Compiler.IParsable<Argument>
{
    public static Argument Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Scalar.Parse(ref parser)
            ?? Scope.Parse(ref parser)
            ?? Reference.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Argument { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Reference(Argument argument) => argument.value as Reference;
    public static implicit operator Scalar(Argument argument) => argument.value as Scalar;
    public static implicit operator Scope(Argument argument) => argument.value as Scope;

    public Syntax value;
}
