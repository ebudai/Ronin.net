// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

/// <summary>
///     Exposes one to the current <see cref="Scope"/> via 'import'
/// </summary>
/// 
/// <example>
///     part of best package for weather lookups
///     
///     import best package for weather lookups
///     import git://github.com/ebudai/Ronin as ronin
/// </example>
internal class Export : Statement
{
    public PartOf Keyword { get; init; }
    public Name Name { get; init; }

    public static new Export Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.TryAdvance<PartOf>(out var keyword) is false) return null;

        if (Name.Parse(ref parser) is not Name identifier) return new ExpectedNameError { Tokens = Parser.Recover(ref current, parser) };

        current = parser;
        return new Export 
        {
            Keyword = keyword,
            Name = identifier
        };
    }

    public class ExpectedNameError : Export, IError
    {
        public string Reason { get; } = "expected name";
        public ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
