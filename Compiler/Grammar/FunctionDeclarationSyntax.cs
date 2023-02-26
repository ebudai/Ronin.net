// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Ordered grouping of instructions to execute when called
/// </summary>
internal class FunctionDeclarationSyntax : Syntax, Compiler.IParsable<FunctionDeclarationSyntax>
{
    public IdentifierSyntax Identifier { get; init; }
    public Modifiers Modifiers { get; init; }
    public Reference Returns { get; init; }
    public Scope Body { get; init; }

    public static FunctionDeclarationSyntax Parse(ref Parser context)
    {
        Parser parser = context;

        if (parser.FailsToConsume<FunctionKeyword>()) return null;

        if (IdentifierSyntax.Parse(ref parser) is not IdentifierSyntax identifier) return null;

        Modifiers modifiers = null;
        Reference returns = null;
        if (parser.CurrentToken is ReturnsSymbol)
        {
            parser.Advance();
            modifiers = Modifiers.Parse(ref parser);
            returns = Reference.Parse(ref parser);
        }

        var body = Scope.Parse(ref parser);

        return new FunctionDeclarationSyntax
        {
            Identifier = identifier,
            Modifiers = modifiers,
            Returns = returns,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
