using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Terminal : Symbol
{
    public const char character = ';';
    public const string symbol = ";";

    private Terminal(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Terminal Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Terminal(lexer) : null;
}
