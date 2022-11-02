using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Hierarchy : Symbol
{
    public const char character = '/';
    public const string symbol = "/";

    private Hierarchy(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Hierarchy Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Hierarchy(lexer) : null;

    internal override bool CanBeUsedInNames => true;
}
