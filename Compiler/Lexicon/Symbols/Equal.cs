using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Equal : Punctuation
{
    public const char character = '=';
    public const string symbol = "=";

    private Equal(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Equal Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Equal(lexer) : null;
}
