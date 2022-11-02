using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class CloseBrace : Close
{
    public const char character = '}';
    public const string symbol = "}";

    private CloseBrace(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new CloseBrace Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new CloseBrace(lexer) : null;
}
