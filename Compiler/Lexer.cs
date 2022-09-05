using Ronin.Tokens;
using Ronin.Tokens.Literals;

namespace Ronin.Compiler;

internal class Lexer
{
    internal Lexer(string sourcecode)
    {
        Sourcecode = sourcecode.AsMemory();
    }

    internal ReadOnlyMemory<char> Sourcecode { get; }

    internal int Cursor { get; set; }
    internal int Line { get; set; } = 1;
    internal string Error { get; set; }
    internal bool IsEmpty => Span.IsEmpty;
    internal int Length => Span.Length;

    internal ReadOnlySpan<char> Span => Sourcecode[Cursor..].Span;
    internal char this[int index] => Span[index];
    internal ReadOnlyMemory<char> this[Range range] => Sourcecode[Cursor..][range];
    
    internal bool StartsWith(string text) => Span.StartsWith(text);
    internal int IndexOfAny(char[] characters) => Span.IndexOfAny(characters);

    internal List<Token> Lex()
    {
        List<Token> tokens = new();

        while (Cursor < Sourcecode.Length)
        {
            var token = Lex<Whitespace>()
                ?? Lex<TextLiteral>()
                ?? Lex<Comment>()
                ?? Lex<CharLiteral>()
                ?? Lex<HexLiteral>()
                ?? Lex<BinaryLiteral>()
                ?? Lex<DateLiteral>()
                ?? Lex<TimeLiteral>()
                ?? Lex<MoneyLiteral>()
                ?? Lex<NumberLiteral>()
                ?? Lex<IntegerLiteral>()
                ?? Lex<UrlLiteral>()
                ?? Lex<Symbol>()
                ?? Lex<Keyword>()
                ?? Lex<Name>()
                ?? Lex<Error>() as Token;
            tokens.Add(token);
        }

        return tokens;
    }

    private T Lex<T>() where T : Token, ILexable<T> => T.Lex(this);
}
