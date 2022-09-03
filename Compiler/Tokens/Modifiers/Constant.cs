using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Constant : Token, ILexable<Constant>
{
    public Constant(Lexer lexer, int length) : base(lexer, length) { }

    public static Constant Lex(Lexer lexer)
    {
        throw new NotImplementedException();
    }
}
