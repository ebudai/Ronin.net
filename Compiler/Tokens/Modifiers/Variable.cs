using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal class Variable : Token, ILexable<Variable>
{
    private const string keyword = "var"; 
    
    public Variable(Lexer lexer) : base(lexer, keyword.Length) { }
    
    public static Variable Lex(Lexer lexer) => lexer.IsModifier(keyword) ? new Variable(lexer) : null;
}
