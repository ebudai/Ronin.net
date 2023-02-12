using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Range : Punctuation
{
    public const string symbol = "..";

    public static new Range Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.DoesNotStartWith(symbol)) return null;
        return new Range { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
