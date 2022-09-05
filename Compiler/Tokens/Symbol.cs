using Ronin.Compiler;

namespace Ronin.Tokens;

internal class Symbol : Token, ILexable<Symbol>
{
    public Symbol(Lexer lexer, int length) : base(lexer, length) { }

    public static Symbol Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        for (int i = 0, max = _symbols.Length; i != max; ++i)
        {
            ref var symbol = ref _symbols[i];
            if (lexer.StartsWith(symbol)) return new Symbol(lexer, symbol.Length);
        }
        return null;
    }

    private static readonly string[] _symbols = { "(", "[", "{", "}", "]", ")", ",", ";" };
}
