using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Asterisk : Symbol
{
    public const char character = '*';
    public const string symbol = "*";

    private Asterisk(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Asterisk Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Asterisk(lexer) : null;
}
