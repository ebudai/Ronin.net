using Ronin.Compiler;

namespace Ronin.Token.Delimiter;

internal class OpenSquareBracket : Open
{
    public const char character = '[';

    public OpenSquareBracket(Lexer lexer) : base(lexer, 1) { }

    public static new OpenSquareBracket Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new OpenSquareBracket(lexer) : null;
}
