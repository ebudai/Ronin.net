using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Interval : Punctuation
{
    public const string symbol = "..";

    private Interval(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Interval Lex(Lexer lexer) => lexer.IsNotEmpty && lexer.StartsWith(symbol) ? new Interval(lexer) : null;
}
