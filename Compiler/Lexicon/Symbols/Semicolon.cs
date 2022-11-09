using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Semicolon : Punctuation
{
    public const char character = ';';
    public const string symbol = ";";

    private Semicolon(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Semicolon Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Semicolon(lexer) : null;
}
