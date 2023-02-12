using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Exclamation : Symbol
{
    public const char character = '!';
    public const string symbol = "!";

    public static new Exclamation Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Exclamation { Sourcecode = lexer.Commit(symbol.Length) };
    }
}
