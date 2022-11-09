using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Comma : Symbol
{
    public const char character = ',';
    public const string symbol = ",";

    private Comma(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Comma Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Comma(lexer) : null;
}
