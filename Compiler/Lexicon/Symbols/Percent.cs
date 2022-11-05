using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Percent : Symbol
{
    public const char character = '%';
    public const string symbol = "%";

    private Percent(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Percent Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Percent(lexer) : null;
}
