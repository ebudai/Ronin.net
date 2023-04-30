// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     Restricts a <see cref="Datum"/> to a particular shape of data
///     resulting from evaluation of a <see cref="Function"/> or <see cref="Datum"/>
/// </summary>
/// 
/// <example>
///     datatype Car = Vehicle and { var speed => number; var price => money; }
/// </example>
internal class Datatype : Statement, IParsableSyntax<Datatype>
{
    public Identifier Identifier { get; init; }
    public Reference Algebra { get; init; }
    public Scope Body { get; init; }

    public new static Datatype Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.TryConsume<Lexicon.Keyword.Datatype>() is false) return null;

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        Reference algebra = null;
        if (parser.Token is Assign)
        {
            parser.Advance();
            algebra = Reference.Parse(ref parser);
        }

        var body = Scope.Parse(ref parser);

        return new Datatype
        {
            Identifier = identifier,
            Algebra = algebra,
            Body = body,
            Source = parser.Commit(ref current)
        };
    }
}