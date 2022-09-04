using Ronin.Compiler;

namespace Unit;

public class CloseSquareBracket
{
    [Fact(DisplayName = "close square bracket")]
    public void Basic()
    {
        const string sourcecode = "]";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Tokens.Symbols.CloseSquareBracket.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(sourcecode.ToArray(), lexed.Sourcecode.ToArray());
    }
}
