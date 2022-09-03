using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Datatype : Token, ILexable<Datatype>
{
    public Datatype(Lexer lexer, int length) : base(lexer, length) { }

    public static Datatype Lex(Lexer lexer)
    {
        const string keyword = "datatype ";

        if (lexer.Sourcecode.Length < keyword.Length) return null;

        return lexer.Sourcecode.Span.StartsWith(keyword) ? new Datatype(lexer, 1) : null;
    }
}
