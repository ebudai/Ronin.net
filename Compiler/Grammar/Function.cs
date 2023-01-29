// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Ordered grouping of instructions to execute when called
/// </summary>
internal class Function : Syntax, Compiler.IParsable<Function>
{
    public Modifiers Is { get; init; }
    public Identifier Identifier { get; init; }
    public Reference ReturnType { get; init; }
    public Scope Body { get; init; }

    public static Function Parse(ref Parser context)
    {
        Parser parser = context;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.Current is not Lexicon.Keywords.Function) return null;
        
        parser.Advance();

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        Reference returnType = null;
        if (parser.Current is Returns)
        {
            parser.Advance();
            returnType = Reference.Parse(ref parser);
            if (returnType is null) return null;
        }

        if (Scope.Parse(ref parser) is not Scope body) return null;

        return new Function
        {
            Is = modifiers,
            Identifier = identifier,
            ReturnType = returnType,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
