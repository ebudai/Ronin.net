using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Minus : Symbol
{
    public const char character = '-';
    public const string symbol = "-";

    private Minus(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Minus Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Minus(lexer) : null;
}
