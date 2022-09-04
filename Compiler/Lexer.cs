using Ronin.Tokens;
using Ronin.Tokens.Literals;
using Ronin.Tokens.Modifiers;
using Ronin.Tokens.Symbols;

namespace Ronin.Compiler;

internal class Lexer
{
    internal Lexer(string sourcecode)
    {
        Sourcecode = sourcecode;
        _span = Sourcecode.AsMemory();
    }

    internal string Sourcecode { get; }

    internal int Cursor 
    { 
        get => _cursor; 
        set
        {
            _cursor = value;
            _span = Sourcecode.AsMemory()[value..];            
        }
    }

    internal int Line { get; set; }
    internal string Error { get; set; }
    internal bool IsEmpty => _span.IsEmpty;
    internal int Length => _span.Length;

    internal char this[int index] => _span.Span[Cursor..][index];
    internal ReadOnlyMemory<char> this[Range range] => _span[range];
    
    private int _cursor = 0;
    private ReadOnlyMemory<char> _span;

    internal bool StartsWith(string text) => _span.Span.StartsWith(text);
    internal int IndexOf(char character) => _span.Span.IndexOf(character);

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
                ?? Lex<DateTimeLiteral>()
                ?? Lex<DateLiteral>()
                ?? Lex<TimeLiteral>()
                ?? Lex<MoneyLiteral>()
                ?? Lex<NumberLiteral>()
                ?? Lex<IntegerLiteral>()
                ?? Lex<UrlLiteral>()
                ?? Lex<OpenParenthesis>()
                ?? Lex<OpenSquareBracket>()
                ?? Lex<OpenBrace>()
                ?? Lex<CloseParenthesis>()
                ?? Lex<CloseSquareBracket>()
                ?? Lex<CloseBrace>()
                ?? Lex<Separator>()
                ?? Lex<Terminal>()
                ?? Lex<Compiled>()
                ?? Lex<Constant>()
                ?? Lex<Datatype>()
                ?? Lex<Function>()
                ?? Lex<Reactive>()
                ?? Lex<Variable>()
                ?? Lex<Name>() as Token;
            if (token is null)
            {
                Error = "unknown token";
                return tokens;
            }

            tokens.Add(token);
        }

        return tokens;
    }

    private T Lex<T>() where T : Token, ILexable<T> => T.Lex(this);
}
