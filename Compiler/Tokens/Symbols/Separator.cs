using Ronin.Compiler;

namespace Ronin.Tokens.Symbols;

internal class Separator : Token, ILexable<Separator>
{
    public Separator(Lexer lexer) : base(lexer, 1) { }

    public static Separator Lex(Lexer lexer) => lexer.IsEmpty || lexer[0] is not ',' ? null : new Separator(lexer);
}