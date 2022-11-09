using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Returns : Punctuation
{
    public const string symbol = "=>";

    private Returns(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Returns Lex(Lexer lexer) => lexer.IsNotEmpty && lexer.StartsWith(symbol) ? new Returns(lexer) : null;
}
