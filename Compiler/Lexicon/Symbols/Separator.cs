using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Separator : Symbol
{
    public const char character = ',';
    public const string symbol = ",";

    private Separator(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Separator Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Separator(lexer) : null;
}
