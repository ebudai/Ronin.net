using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class GreaterThan : Symbol
{
    public const char character = '>';
    public const string symbol = ">";

    private GreaterThan(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new GreaterThan Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new GreaterThan(lexer) : null;
}
