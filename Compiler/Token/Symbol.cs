using Ronin.Compiler;

namespace Ronin.Token;

internal class Symbol : Lexeme
{
    internal const char separator = ',';
    internal const char terminal = ';';
    internal const char hierarchy = '/';
    internal const char assign = '=';
    internal const string returns = "=>";

    internal Symbol(Lexer lexer, int length) : base(lexer, length) { }

    internal bool IsOpenBrace => Sourcecode.Span[0] is '{';
    internal bool IsCloseBrace => Sourcecode.Span[0] is '}';
    internal bool IsOpenSquareBracket => Sourcecode.Span[0] is '[';
    internal bool IsCloseSquareBracket => Sourcecode.Span[0] is ']';
    internal bool IsOpenParenthesis => Sourcecode.Span[0] is '(';
    internal bool IsCloseParenthesis => Sourcecode.Span[0] is ')';
    internal bool IsSeparator => Sourcecode.Span[0] is separator;
    internal bool IsTerminal => Sourcecode.Span[0] is terminal;
    internal bool IsCharacterDelimiter => Sourcecode.Span[0] is '\'';
    internal bool IsTextDelimiter => Sourcecode.Span[0] is '"';
    internal bool IsReturns => Sourcecode.Span[0] is assign && Sourcecode.Span.Length is >= 2 && Sourcecode.Span[1] is '>';
    internal bool IsAssign => Sourcecode.Span[0] is assign && (Sourcecode.Span.Length is not >= 2 || Sourcecode.Span[1] is not '>');

    internal bool IsOpen => IsOpenBrace || IsOpenParenthesis || IsOpenSquareBracket;
    internal bool IsClose => IsCloseBrace || IsCloseParenthesis || IsCloseSquareBracket;

    internal static Symbol Lex(Lexer lexer)
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

    private static readonly string[] _symbols = { "(", "[", "{", "}", "]", ")", ",", ";", "'", "\"", "=>", "=" };
}
