// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

/// <summary>
///     Ordered grouping of instructions to execute when called
/// </summary>
internal class Function : Syntax, Compiler.IParsable<Function>
{
    public Modifiers Is { get; private init; }
    public Identifier Identifier { get; private init; }
    public Scope Body { get; private init; }

    public static Function Parse(ref Parser context)
    {
        Parser parser = context;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.Current is not Lexicon.Keywords.Function) return null;
        
        parser.Advance();

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        if (Scope.Parse(ref parser) is not Scope body) return null;

        return new Function
        {
            Is = modifiers,
            Identifier = identifier,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
