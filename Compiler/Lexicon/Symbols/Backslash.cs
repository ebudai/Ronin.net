using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Backslash : Symbol
{
    public const char character = '\\';
    public const string symbol = "\\";

    private Backslash(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Backslash Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Backslash(lexer) : null;
}
