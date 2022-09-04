using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Constant : Token, ILexable<Constant>
{
    private const string keyword = "constant";

    public Constant(Lexer lexer) : base(lexer, keyword.Length) { }

    public static Constant Lex(Lexer lexer) => lexer.IsModifier(keyword) ? new Constant(lexer) : null;
}
