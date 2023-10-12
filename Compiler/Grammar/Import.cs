using Ronin.Compiler;
using Ronin.Lexicon;
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
    public Module Module { get; init; }

    public static new Import Parse(ref Parser current)
    {
        if (current.Token is not Lexicon.Import keyword) return null;

        Parser parser = current;
        parser.Advance();

        if (Name.Parse(ref parser) is not Name name)
        {
            return new ExpectedNameError { Tokens = Unknown.Parse(ref current).Tokens };
        }

        current = parser;
        return new Import
        {
            Keyword = keyword,
            Module = new Module.Unresolved { Name = name }
        };
    }

    public override void ResolveTypes(Scope context)
    {
        Module.ResolveTypes(context);
    }

    public class ExpectedNameError : Import, IError
    {
        public string Reason { get; } = "expected name";
        public ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
