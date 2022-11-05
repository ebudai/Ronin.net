using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Plus : Symbol
{
    public const char character = '+';
    public const string symbol = "+";

    private Plus(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Plus Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Plus(lexer) : null;
}
