using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Pipe : Symbol
{
    public const char character = '|';
    public const string symbol = "|";

    private Pipe(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Pipe Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Pipe(lexer) : null;
}
