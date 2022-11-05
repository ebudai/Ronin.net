using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Backtick : Symbol
{
    public const char character = '`';
    public const string symbol = "`";

    private Backtick(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Backtick Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Backtick(lexer) : null;
}
