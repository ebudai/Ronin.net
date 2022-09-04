using Ronin.Compiler;

namespace Unit;

public class OpenParenthesis
{
    [Fact(DisplayName = "open parenthesis")]
    public void Basic()
    {
        const string sourcecode = "(";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Tokens.Symbols.OpenParenthesis.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }
}
