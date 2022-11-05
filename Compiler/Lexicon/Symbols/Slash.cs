using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Slash : Symbol
{
    public const char character = '/';
    public const string symbol = "/";

    private Slash(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Slash Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Slash(lexer) : null;
}
