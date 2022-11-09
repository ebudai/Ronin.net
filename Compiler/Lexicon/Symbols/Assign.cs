using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Assign : Punctuation
{
    public const char character = '=';
    public const string symbol = "=";

    private Assign(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Assign Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Assign(lexer) : null;
}
