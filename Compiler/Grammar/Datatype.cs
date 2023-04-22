// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     Restricts a <see cref="Datum"/> or the <see cref="Temporary"/> 
///     resulting from evaluation of a <see cref="Function"/> to a specific type of data
/// </summary>
/// 
/// <example>
///     datatype Car = Vehicle and { var speed => number; var price => money; }
/// </example>
internal class Datatype : Syntax, IParsableSyntax<Datatype>
{
    public Identifier Identifier { get; init; }
    public Reference Algebra { get; init; }
    public Scope Body { get; init; }

    public static Datatype Parse(ref Parser context)
    {
        Parser parser = context;

        if (parser.FailsToConsume<Lexicon.Keyword.Datatype>()) return null;

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        Reference algebra = null;
        if (parser.CurrentToken is Assign)
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
            Source = parser.Commit(ref context)
        };
    }
}
