// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     A singular piece of data residing in memory, and declared in a <see cref="Scope"/>
/// </summary>
/// 
/// <example>
///     datatype Building
///     {
///         var floors => number;
///         ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
///     }
/// </example>
internal class DatumDeclarationSyntax : Syntax, Compiler.IParsable<DatumDeclarationSyntax>
{
    public Keyword Mutability { get; init; }
    public Modifiers Is { get; init; }
    public Name Name { get; init; }
    public Reference Datatype { get; init; }
    public Value Initializer { get; init; }

    public static DatumDeclarationSyntax Parse(ref Parser context)
    {
        Parser parser = context;

        var mutator = parser.CurrentToken is VariableKeyword or ConstantKeyword or ReactiveKeyword ? parser.CurrentToken as Keyword : null;
        if (mutator is not null) parser.Advance();

        if (Name.Parse(ref parser) is not Name name) return null;

        Modifiers modifiers = null;
        Reference datatype = null;
        if (parser.CurrentToken is ReturnsSymbol)
        {
            parser.Advance();
            modifiers = Modifiers.Parse(ref parser);
            datatype = Reference.Parse(ref parser);
        }

        Value initializer = null;
        if (parser.CurrentToken is AssignSymbol)
        {
            parser.Advance();
            initializer = Value.Parse(ref parser);
        }

        return new DatumDeclarationSyntax
        {
            Mutability = mutator,
            Name = name,
            Is = modifiers,
            Datatype = datatype,
            Initializer = initializer,
            Source = parser.Commit(ref context)
        };
    }
}