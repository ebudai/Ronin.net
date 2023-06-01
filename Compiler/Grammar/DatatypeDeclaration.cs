// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

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
    public bool IsExtension { get; init; }
    public Name Identifier { get; init; }
    public Reference Algebra { get; init; }
    public Definition Definition { get; init; }

    public new static DatatypeDeclaration Parse(ref Parser current)
    {
        Parser parser = current;

        bool isExtension = parser.TryAdvance<Extends>();

        if (parser.TryAdvance<Datatype>() is false) return null;

        if (Name.Parse(ref parser) is not Name identifier) return null;

        Reference algebra = null;
        if (parser.Token is Assign)
        {
            parser.Advance();
            algebra = Reference.Parse(ref parser);
        }

        var definition = Definition.Parse(ref parser);

        return new DatatypeDeclaration
        {
            IsExtension = isExtension,
            Identifier = identifier,
            Algebra = algebra,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}