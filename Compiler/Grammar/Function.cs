// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Ordered grouping of instructions to execute when called
/// </summary>
internal class Function : Syntax, Compiler.IParsable<Function>
{
    public Identifier Identifier { get; init; }
    public Modifiers Modifiers { get; init; }
    public Reference Returns { get; init; }
    public Scope Body { get; init; }

    public static Function Parse(ref Parser context)
    {
        Parser parser = context;

        if (parser.Current is not Lexicon.Keywords.Function) return null;        
        parser.Advance();

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        Modifiers modifiers = null;
        Reference returns = null;
        if (parser.Current is Returns)
        {
            parser.Advance();
            modifiers = Modifiers.Parse(ref parser);
            returns = Reference.Parse(ref parser);
            if (returns is null) throw new ExpectedSyntaxError<Word, Literal, OpenParenthesis>(ref context);
        }

        var body = Scope.Parse(ref parser);

        return new Function
        {
            Identifier = identifier,
            Modifiers = modifiers,
            Returns = returns,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
