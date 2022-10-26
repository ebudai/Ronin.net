using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class OpenBrace : Open
{
    public const char character = '{';

    public OpenBrace(Lexer lexer) : base(lexer, 1) { }

    public static new OpenBrace Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new OpenBrace(lexer) : null;
}
