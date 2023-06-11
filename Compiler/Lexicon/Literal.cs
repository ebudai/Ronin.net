// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Literals;

namespace Ronin.Lexicon;

internal class Literal : Token
{
    public static Token Lex(scoped ref Lexer lexer)
        => Character.Lex(ref lexer)
        ?? Date.Lex(ref lexer)
        ?? Time.Lex(ref lexer)
        ?? Money.Lex(ref lexer)
        ?? Number.Lex(ref lexer)
        ?? Text.Lex(ref lexer)
        ?? Url.Lex(ref lexer);
}
