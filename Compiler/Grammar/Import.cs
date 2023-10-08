using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;
using System;

namespace Ronin.Grammar;

/// <summary>
///     Names a <see cref="Scope"/> via 'part of'
/// </summary>
/// 
/// <example>
///     part of best package for weather lookups
///     
///     import best package for weather lookups
///     import git://github.com/ebudai/Ronin as ronin
/// </example>
internal class Import : Statement
{
    public Lexicon.Import Keyword { get; init; }
    public Name Name { get; init; }

    public static new Import Parse(ref Parser current)
    {
        Parser parser = current;
        
        if (current.Token is not Lexicon.Import keyword) return null;
        parser.Advance();

        if (Name.Parse(ref parser) is not Name name) return new ExpectedNameError { Tokens = current.AdvanceTo(ref parser) };

        current = parser;
        return new Import
        {
            Keyword = keyword,
            Name = name
        };
    }

    public class ExpectedNameError : Import, IError
    {
        public string Reason { get; } = "expected name";
        public ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
