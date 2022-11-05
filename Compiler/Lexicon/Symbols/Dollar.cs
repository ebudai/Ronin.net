using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Dollar : Symbol
{
    public const char character = '$';
    public const string symbol = "$";

    private Dollar(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Dollar Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Dollar(lexer) : null;
}
