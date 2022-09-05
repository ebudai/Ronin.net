using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class BinaryLiteral : Token, ILexable<BinaryLiteral>
{
    public BinaryLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static BinaryLiteral Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer[0] is not '0' || lexer[1] is not 'b' and not 'B') return null;
        
        if (lexer.Length is <= 2)
        {
            lexer.Error = "unterminated hex literal"; //TODO make this an error token
            return null;
        }
        int length = 2;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (lexer[i] is '0' or '1' or '_')
            {
                ++length;
                continue;
            }

            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i) || lexer[i] is '.' or '\'' or '"')
            {
                length = i;
                break;
            }

            lexer.Error = $"invalid char '{lexer[i]}' at {i} for binary literal";
            return null;
        }
        return new BinaryLiteral(lexer, length);
    }
}