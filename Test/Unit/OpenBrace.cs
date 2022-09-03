using Ronin.Compiler;

namespace Unit;

public class OpenBrace
{
    [Fact(DisplayName = "open brace")]
    public void Basic()
    {
        const string sourcecode = "{";

        Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.OpenBrace.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
