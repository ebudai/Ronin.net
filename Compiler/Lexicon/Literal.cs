using Ronin.Compiler;
using Ronin.Lexicon.Literals;

namespace Ronin.Lexicon;

internal class Literal : Token
{
    internal Literal(Lexer lexer, int length) : base(lexer, length) { }

    internal static Token Lex(Lexer lexer)
        => Character.Lex(lexer)
        ?? Date.Lex(lexer)
        ?? Time.Lex(lexer)
        ?? Money.Lex(lexer)
        ?? Number.Lex(lexer)
        ?? Text.Lex(lexer)
        ?? Url.Lex(lexer);
}
