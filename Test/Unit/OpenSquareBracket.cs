using Ronin.Compiler;

namespace Unit;

public class OpenSquareBracket
{
    [Fact(DisplayName = "open square bracket")]
    public void Basic()
    {
        const string sourcecode = "[";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Tokens.Symbols.OpenSquareBracket.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }
}
