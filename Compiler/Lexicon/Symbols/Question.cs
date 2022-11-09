using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Question : Symbol
{
    public const char character = '?';
    public const string symbol = "?";

    private Question(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new Question Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new Question(lexer) : null;
}
