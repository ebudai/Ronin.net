using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Compiled : Token, ILexable<Compiled>
{
    private const string keyword = "compiled";

    public Compiled(Lexer lexer) : base(lexer, keyword.Length) { }

    public static Compiled Lex(Lexer lexer) => lexer.IsModifier(keyword) ? new Compiled(lexer) : null;
}
