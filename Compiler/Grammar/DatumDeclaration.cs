// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Ronin.Lexicon.Keywords;

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
///     
///     function do stuff(x => number, y => date) { }
///                       ↑↑↑↑↑↑↑↑↑↑↑  ↑↑↑↑↑↑↑↑↑
/// </example>
internal class DatumDeclaration : Statement, IParsableSyntax<DatumDeclaration>
{
    public Keyword Mutability { get; init; }
    public Modifiers Modifiers { get; init; }
    public Name Name { get; init; }
    public Reference Datatype { get; init; }
    public Value Initializer { get; init; }

    public new static DatumDeclaration Parse(ref Parser current)
    {
        Parser parser = current;

        var mutator = parser.Token is Variable or Constant or Reactive ? parser.Token as Keyword : null;
        if (mutator is not null) parser.Advance();

        if (Words.Parse(ref parser) is not Words name) return null;

        Modifiers modifiers = null;
        Reference datatype = null;
        if (parser.Token is Returns)
        {
            parser.Advance();
            modifiers = Modifiers.Parse(ref parser);
            datatype = Reference.Parse(ref parser);
        }

        Value initializer = null;
        if (parser.Token is Assign)
        {
            parser.Advance();
            initializer = Value.Parse(ref parser);
        }

        return new DatumDeclaration
        {
            Mutability = mutator,
            Name = new Name(name),
            Modifiers = modifiers,
            Datatype = datatype,
            Initializer = initializer,
            Source = parser.Commit(ref current)
        };
    }
}