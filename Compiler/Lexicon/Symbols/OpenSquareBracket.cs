using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class OpenSquareBracket : Open
{
    public const char character = '[';
    public const string symbol = "[";

    private OpenSquareBracket(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new OpenSquareBracket Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new OpenSquareBracket(lexer) : null;
}
