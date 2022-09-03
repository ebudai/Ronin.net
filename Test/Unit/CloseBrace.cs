using Ronin.Compiler;

namespace Unit;

public class CloseBrace
{
    [Fact(DisplayName = "close brace")]
    public void Basic()
    {
        const string sourcecode = "}";

        Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.CloseBrace.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
