using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Ampersand : Symbol
{
    public const char character = '&';
    public const string symbol = "&";

    private Ampersand(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Ampersand Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Ampersand(lexer) : null;
}
