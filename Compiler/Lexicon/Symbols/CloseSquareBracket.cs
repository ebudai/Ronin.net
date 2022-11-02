using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class CloseSquareBracket : Close
{
    public const char character = ']';
    public const string symbol = "]";

    private CloseSquareBracket(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new CloseSquareBracket Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new CloseSquareBracket(lexer) : null;
}
