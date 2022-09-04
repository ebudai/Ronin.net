using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Reactive : Token, ILexable<Reactive>
{
    private const string keyword = "reactive"; 
    
    public Reactive(Lexer lexer) : base(lexer, keyword.Length) { }
    
    public static Reactive Lex(Lexer lexer) => lexer.IsModifier(keyword) ? new Reactive(lexer) : null;
}
