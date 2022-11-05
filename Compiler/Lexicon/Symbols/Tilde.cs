using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Tilde : Symbol
{
    public const char character = '~';
    public const string symbol = "~";

    private Tilde(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Tilde Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Tilde(lexer) : null;
}
