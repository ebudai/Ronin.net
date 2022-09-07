using Ronin.Compiler;

namespace Ronin.Token;

internal class Symbol : Token
{
    internal Symbol(Lexer lexer, int length) : base(lexer, length) { }

    internal bool IsOpenBrace => Sourcecode.Span[0] is '{';
    internal bool IsCloseBrace => Sourcecode.Span[0] is '}';
    internal bool IsOpenSquareBracket => Sourcecode.Span[0] is '[';
    internal bool IsCloseSquareBracket => Sourcecode.Span[0] is ']';
    internal bool IsOpenParenthesis => Sourcecode.Span[0] is '(';
    internal bool IsCloseParenthesis => Sourcecode.Span[0] is ')';
    internal bool IsSeparator => Sourcecode.Span[0] is ',';
    internal bool IsTerminal => Sourcecode.Span[0] is ';';
    internal bool IsCharacterDelimiter => Sourcecode.Span[0] is '\'';
    internal bool IsTextDelimiter => Sourcecode.Span[0] is '"';
    internal bool IsReturns //=> Sourcecode.Span[0] is '=' && Sourcecode.Span[1] is '>';
    {
        get
        {
            if (Sourcecode.Span[0] is '=')
            {
                return Sourcecode.Span[1] is '>';
            }
            return false;
        }
    }

    internal static Token Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        for (int i = 0, max = _symbols.Length; i != max; ++i)
        {
            ref var symbol = ref _symbols[i];
            if (lexer.StartsWith(symbol)) return new Symbol(lexer, symbol.Length);
        }
        return null;
    }

    internal static bool IsSymbol(Lexer lexer, int i = 0) => _symbols.Any(symbol => lexer[i..].Span.StartsWith(symbol));    

    private static readonly string[] _symbols = { "(", "[", "{", "}", "]", ")", ",", ";", "'", "\"", "=>" }; //TODO try to add ' and "
}
