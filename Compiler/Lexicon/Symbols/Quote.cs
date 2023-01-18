using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Quote : Punctuation
{
    public const char character = '\'';
    public const string symbol = "'";

    private Quote(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Quote Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Quote(lexer) : null;
}
