using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Chevron : Symbol
{
    public const char character = '^';
    public const string symbol = "^";

    private Chevron(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Chevron Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Chevron(lexer) : null;
}
