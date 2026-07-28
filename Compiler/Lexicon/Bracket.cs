using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Bracket : Punctuation
{
    public new static Bracket Lex(ref Lexer lexer) => Open.Lex(ref lexer) ?? Close.Lex(ref lexer) as Bracket;
}

internal class Open : Bracket
{
    public new static Open Lex(ref Lexer lexer)
        => SquareBracket.Lex(ref lexer)
        ?? Brace.Lex(ref lexer)
        ?? Parenthesis.Lex(ref lexer) as Open;

    internal class SquareBracket : Open
    {
        internal const char symbol = '[';

        public static new SquareBracket Lex(ref Lexer lexer) => Lex<SquareBracket>(ref lexer, symbol);
    }

    internal class Brace : Open
    {
        internal const char symbol = '{';

        public static new Brace Lex(ref Lexer lexer) => Lex<Brace>(ref lexer, symbol);
    }

    internal class Parenthesis : Open
    {
        internal const char symbol = '(';

        public static new Parenthesis Lex(ref Lexer lexer) => Lex<Parenthesis>(ref lexer, symbol);
    }

}

internal class Close : Bracket
{
    public new static Close Lex(ref Lexer lexer)
        => SquareBracket.Lex(ref lexer)
        ?? Brace.Lex(ref lexer)
        ?? Parenthesis.Lex(ref lexer) as Close;

    internal class SquareBracket : Close
    {
        internal const char symbol = ']';

        public static new SquareBracket Lex(ref Lexer lexer) => Lex<SquareBracket>(ref lexer, symbol);
    }

    internal class Brace : Close
    {
        internal const char symbol = '}';

        public static new Brace Lex(ref Lexer lexer) => Lex<Brace>(ref lexer, symbol);
    }

    internal class Parenthesis : Close
    {
        internal const char symbol = ')';

        public static new Parenthesis Lex(ref Lexer lexer) => Lex<Parenthesis>(ref lexer, symbol);
    }
}
