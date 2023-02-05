// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

using Arguments = Ronin.Grammar.Aggregates.Arguments;

namespace Ronin.Grammar;

/// <summary>
///     Union of <see cref="Scalar"/>, <see cref="Arguments"/>, and <see cref="Scope"/>
/// </summary>
/*internal class Temporary : Syntax, Compiler.IParsable<Temporary>
{
    public static Temporary Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Scalar.Parse(ref parser)
            ?? Arguments.Parse(ref parser)
            ?? Scope.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Temporary { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Scalar(Temporary value) => value.value as Scalar;
    public static implicit operator Arguments(Temporary value) => value.value as Arguments;
    public static implicit operator Scope(Temporary value) => value.value as Scope;

    private Syntax value;
}*/
