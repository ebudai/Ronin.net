using Ronin.Compiler;

namespace Unit;

public class CloseSquareBracket
{
    [Fact(DisplayName = "close square bracket")]
    public void Basic()
    {
        const string sourcecode = "]";

        Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.CloseSquareBracket.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
