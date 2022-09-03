using Ronin.Compiler;

namespace Unit;

public class OpenSquareBracket
{
    [Fact(DisplayName = "open square bracket")]
    public void Basic()
    {
        const string sourcecode = "[";

        Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.OpenSquareBracket.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
