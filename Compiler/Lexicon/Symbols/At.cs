using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class At : Symbol
{
    public const char character = '@';
    public const string symbol = "@";

    private At(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new At Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new At(lexer) : null;
}
