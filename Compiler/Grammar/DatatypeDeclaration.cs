// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     Restricts a <see cref="DatumDeclaration"/> to a particular shape of data
///     resulting from evaluation of a <see cref="FunctionDeclaration"/> or <see cref="DatumDeclaration"/>
/// </summary>
/// 
/// <example>
///     datatype Car = Vehicle and { var speed => number; var price => money; }
/// </example>
internal class DatatypeDeclaration : Statement, IParsableSyntax<DatatypeDeclaration>
{
    public Identifier Identifier { get; init; }
    public Reference Algebra { get; init; }
    public Scope Body { get; init; }

    public new static DatatypeDeclaration Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.TryAdvance<Lexicon.Keyword.Datatype>() is false) return null;

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        Reference algebra = null;
        if (parser.Token is Assign)
        {
            parser.Advance();
            algebra = Reference.Parse(ref parser);
        }

        var body = Scope.Parse(ref parser);

        return new DatatypeDeclaration
        {
            Identifier = identifier,
            Algebra = algebra,
            Body = body,
            Source = parser.Commit(ref current)
        };
    }
}