// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Literal : Token
{
    public static Token Lex(ref Lexer lexer)
        => CharacterLiteral.Lex(ref lexer)
        ?? DateLiteral.Lex(ref lexer)
        ?? TimeLiteral.Lex(ref lexer)
        ?? MoneyLiteral.Lex(ref lexer)
        ?? NumberLiteral.Lex(ref lexer)
        ?? TextLiteral.Lex(ref lexer)
        ?? UrlLiteral.Lex(ref lexer);
}
