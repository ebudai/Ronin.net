using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class CloseParenthesis : Close
{
    public const char character = ')';
    public const string symbol = ")";

    private CloseParenthesis(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new CloseParenthesis Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new CloseParenthesis(lexer) : null;
}
