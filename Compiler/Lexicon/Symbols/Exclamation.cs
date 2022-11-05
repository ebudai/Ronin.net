using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Exclamation : Symbol
{
    public const char character = '!';
    public const string symbol = "!";

    private Exclamation(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Exclamation Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Exclamation(lexer) : null;
}
