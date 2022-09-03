using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Reactive : Token, ILexable<Reactive>
{
    public Reactive(Lexer lexer, int length) : base(lexer, length) { }

    public static Reactive Lex(Lexer lexer)
    {
        const string keyword = "reactive ";

        if (lexer.Sourcecode.Length < keyword.Length) return null;

        return lexer.Sourcecode.Span.StartsWith(keyword) ? new Reactive(lexer, 1) : null;
    }
}
