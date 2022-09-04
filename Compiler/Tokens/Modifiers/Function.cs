using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Function : Token, ILexable<Function>
{
    private const string keyword = "function";

    public Function(Lexer lexer) : base(lexer, keyword.Length) { }

    public static Function Lex(Lexer lexer) => lexer.IsModifier(keyword) ? new Function(lexer) : null;
}
