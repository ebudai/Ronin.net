using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Separator : Symbol
{
    public const char character = ',';

    public Separator(Lexer lexer) : base(lexer, 1) { }

    public static new Separator Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new Separator(lexer) : null;
}
