using Ronin.Compiler;

namespace Unit;

public class CloseParenthesis
{
    [Fact(DisplayName = "close parenthesis")]
    public void Basic()
    {
        const string sourcecode = ")";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.CloseParenthesis.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }
}
