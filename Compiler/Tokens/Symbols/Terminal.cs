using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class Terminal : Token, ILexable<Terminal>
{
    public Terminal(Lexer lexer) : base(lexer, 1) { }

    public static Terminal Lex(Lexer lexer) => lexer.IsEmpty || lexer[0] is not ';' ? null : new Terminal(lexer);
}
