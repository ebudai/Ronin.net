using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class TextDelimiter : Punctuation
{
    public const char character = '"';
    public const string symbol = "\"";

    private TextDelimiter(Lexer lexer) : base(lexer, 1) { }

    public static new TextDelimiter Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new TextDelimiter(lexer) : null;
}
