using Ronin.Compiler;

namespace Unit;

public class OpenParenthesis
{
    [Fact(DisplayName = "open parenthesis")]
    public void Basic()
    {
        const string sourcecode = "(";

        Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.OpenParenthesis.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
