using Ronin.Compiler;

namespace Ronin.Token.Value;

internal class Binary : Literal
{
    public Binary(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer.Length is <= 2) return null;
        if (lexer[0] is not '0' || lexer[1] is not 'b' and not 'B') return null;        

        int length = 2;
        while (length != lexer.Length && lexer[length] is '0' or '1' or '_') ++length;

        return new Binary(lexer, length);
    }
}
