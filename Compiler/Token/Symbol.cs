using Ronin.Compiler;

namespace Ronin.Token;

internal class Symbol : Token
{
    internal Symbol(Lexer lexer, int length) : base(lexer, length) { }

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

    private static readonly string[] _symbols = { "(", "[", "{", "}", "]", ")", ",", ";", "'", "\"" }; //TODO try to add ' and "
}
