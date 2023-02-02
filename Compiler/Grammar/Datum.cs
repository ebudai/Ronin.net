// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

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
internal class Datum : Syntax, Compiler.IParsable<Datum>
{
    public Keyword Mutability { get; init; }
    public Modifiers Is { get; init; }
    public Name Name { get; init; }
    public Reference Datatype { get; init; }
    public Value Initializer { get; init; }

    public static Datum Parse(ref Parser context)
    {
        Parser parser = context;

        var mutator = parser.CurrentToken is Variable or Constant or Reactive ? parser.CurrentToken as Keyword : null;
        if (mutator is not null) parser.Advance();

        if (Name.Parse(ref parser) is not Name name) return null;

        Modifiers modifiers = null;
        Reference datatype = null;
        if (parser.CurrentToken is Returns)
        {
            parser.Advance();

            modifiers = Modifiers.Parse(ref parser);

            datatype = Reference.Parse(ref parser);

            if (datatype is null) throw new UnspecifiedDatatypeError(ref context);
        }

        Value initializer = null;
        if (parser.CurrentToken is Assign)
        {
            parser.Advance();
            initializer = Value.Parse(ref parser);
        }

        if (datatype is null && initializer is null) throw new UnspecifiedDatatypeError(ref context);

        return new Datum
        {
            Mutability = mutator,
            Name = name,
            Is = modifiers ?? new(),
            Datatype = datatype,
            Initializer = initializer,
            Source = parser.Commit(ref context)
        };
    }
}