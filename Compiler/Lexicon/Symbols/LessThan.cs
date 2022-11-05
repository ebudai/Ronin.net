using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class LessThan : Symbol
{
    public const char character = '<';
    public const string symbol = "<";

    private LessThan(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new LessThan Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new LessThan(lexer) : null;
}
