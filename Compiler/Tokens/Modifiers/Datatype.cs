using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Datatype : Token, ILexable<Datatype>
{
    private const string keyword = "datatype";

    public Datatype(Lexer lexer) : base(lexer, keyword.Length) { }

    public static Datatype Lex(Lexer lexer) => lexer.IsModifier(keyword) ? new Datatype(lexer) : null;
}
