// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Restricts a <see cref="DatumDeclarationSyntax"/> or the <see cref="Temporary"/> 
///     resulting from evaluation of a <see cref="FunctionDeclarationSyntax"/> to a specific type of data
/// </summary>
/// 
/// <example>
///     datatype Car = Vehicle and { var speed => number; var price => money; }
/// </example>
internal class DatatypeDeclarationSyntax : Syntax, Compiler.IParsable<DatatypeDeclarationSyntax>
{
    public IdentifierSyntax Identifier { get; init; }
    public Reference Algebra { get; init; }
    public Scope Body { get; init; }

    public static DatatypeDeclarationSyntax Parse(ref Parser context)
    {
        Parser parser = context;

        if (parser.FailsToConsume<DatatypeKeyword>()) return null;

        if (IdentifierSyntax.Parse(ref parser) is not IdentifierSyntax identifier) return null;

        Reference algebra = null;
        if (parser.CurrentToken is AssignSymbol)
        {
            parser.Advance();
            algebra = Reference.Parse(ref parser);
        }

        var body = Scope.Parse(ref parser);

        return new DatatypeDeclarationSyntax
        {
            Identifier = identifier,
            Algebra = algebra,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
