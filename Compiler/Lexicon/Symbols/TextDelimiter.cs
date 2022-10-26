using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class TextDelimiter : Symbol
{
    public const char character = '"';

    public TextDelimiter(Lexer lexer) : base(lexer, 1) { }

    public static new TextDelimiter Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new TextDelimiter(lexer) : null;
}
