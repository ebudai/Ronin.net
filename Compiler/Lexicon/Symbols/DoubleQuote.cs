using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class DoubleQuote : Punctuation
{
    public const char character = '"';
    public const string symbol = "\"";

    private DoubleQuote(Lexer lexer) : base(lexer, 1) { }

    public static new DoubleQuote Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new DoubleQuote(lexer) : null;
}
