using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Pound : Symbol
{
    public const char character = '#';
    public const string symbol = "#";

    private Pound(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Pound Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Pound(lexer) : null;
}
