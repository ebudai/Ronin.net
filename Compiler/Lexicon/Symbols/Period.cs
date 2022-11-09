using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Period : Symbol
{
    public const char character = '.';
    public const string symbol = ".";

    private Period(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Period Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Period(lexer) : null;
}
