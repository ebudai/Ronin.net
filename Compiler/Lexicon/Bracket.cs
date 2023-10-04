using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Bracket : Punctuation
{
    public new static Bracket Lex(ref Lexer lexer) 
        => CloseSquareBracket.Lex(ref lexer)
        ?? CloseBrace.Lex(ref lexer)
        ?? CloseParenthesis.Lex(ref lexer)
        ?? OpenSquareBracket.Lex(ref lexer)
        ?? OpenBrace.Lex(ref lexer)
        ?? OpenParenthesis.Lex(ref lexer) as Bracket;
}

internal class CloseSquareBracket : Bracket
{
    internal const char symbol = ']';

    public static new CloseSquareBracket Lex(ref Lexer lexer) => Lex<CloseSquareBracket>(ref lexer, symbol);
}

internal class CloseBrace : Bracket
{
    internal const char symbol = '}';

    public static new CloseBrace Lex(ref Lexer lexer) => Lex<CloseBrace>(ref lexer, symbol);
}

internal class CloseParenthesis : Bracket
{
    internal const char symbol = ')';

    public static new CloseParenthesis Lex(ref Lexer lexer) => Lex<CloseParenthesis>(ref lexer, symbol);
}

internal class OpenSquareBracket : Bracket
{
    internal const char symbol = '[';

    public static new OpenSquareBracket Lex(ref Lexer lexer) => Lex<OpenSquareBracket>(ref lexer, symbol);
}

internal class OpenBrace : Bracket
{
    internal const char symbol = '{';

    public static new OpenBrace Lex(ref Lexer lexer) => Lex<OpenBrace>(ref lexer, symbol);
}

internal class OpenParenthesis : Bracket
{
    internal const char symbol = '(';

    public static new OpenParenthesis Lex(ref Lexer lexer) => Lex<OpenParenthesis>(ref lexer, symbol);
}