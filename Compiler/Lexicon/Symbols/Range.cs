using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Range : Punctuation
{
    public const string symbol = "..";

    private Range(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Range Lex(Lexer lexer) => lexer.IsNotEmpty && lexer.StartsWith(symbol) ? new Range(lexer) : null;
}
