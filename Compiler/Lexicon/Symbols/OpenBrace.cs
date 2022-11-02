using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class OpenBrace : Open
{
    public const char character = '{';
    public const string symbol = "{";

    private OpenBrace(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new OpenBrace Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new OpenBrace(lexer) : null;
}
