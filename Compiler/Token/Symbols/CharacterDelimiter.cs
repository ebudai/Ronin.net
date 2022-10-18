using Ronin.Compiler;

namespace Ronin.Token.Symbols;

internal class CharacterDelimiter : Symbol
{
    public const char character = '\'';

    public CharacterDelimiter(Lexer lexer) : base(lexer, 1) { }

    public static new CharacterDelimiter Lex(Lexer lexer) => !lexer.IsEmpty && lexer[0] is character ? new CharacterDelimiter(lexer) : null;
}
