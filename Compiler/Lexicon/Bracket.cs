using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Bracket : Punctuation
{
    public new static Bracket Lex(ref Lexer lexer) => Open.Lex(ref lexer) ?? Close.Lex(ref lexer) as Bracket;
}

internal class Open : Bracket
{
    public new static Open Lex(ref Lexer lexer)
        => OpenSquareBracket.Lex(ref lexer)
        ?? OpenBrace.Lex(ref lexer)
        ?? OpenParenthesis.Lex(ref lexer) as Open;
}

internal class Close : Bracket
{
    public new static Close Lex(ref Lexer lexer)
        => CloseSquareBracket.Lex(ref lexer)
        ?? CloseBrace.Lex(ref lexer)
        ?? CloseParenthesis.Lex(ref lexer) as Close;
}

internal class CloseSquareBracket : Close
{
    internal const char symbol = ']';

    public static new CloseSquareBracket Lex(ref Lexer lexer) => Lex<CloseSquareBracket>(ref lexer, symbol);
}

internal class CloseBrace : Close
{
    internal const char symbol = '}';

    public static new CloseBrace Lex(ref Lexer lexer) => Lex<CloseBrace>(ref lexer, symbol);
}

internal class CloseParenthesis : Close
{
    internal const char symbol = ')';

    public static new CloseParenthesis Lex(ref Lexer lexer) => Lex<CloseParenthesis>(ref lexer, symbol);
}

internal class OpenSquareBracket : Open
{
    internal const char symbol = '[';

    public static new OpenSquareBracket Lex(ref Lexer lexer) => Lex<OpenSquareBracket>(ref lexer, symbol);
}

internal class OpenBrace : Open
{
    internal const char symbol = '{';

    public static new OpenBrace Lex(ref Lexer lexer) => Lex<OpenBrace>(ref lexer, symbol);
}

internal class OpenParenthesis : Open
{
    internal const char symbol = '(';

    public static new OpenParenthesis Lex(ref Lexer lexer) => Lex<OpenParenthesis>(ref lexer, symbol);
}