using Ronin.Compiler;

namespace Unit;

public class Separator
{
    [Fact(DisplayName = "separator")]
    public void Basic()
    {
        const string sourcecode = ",";

        Lexer lexer = new() { Sourcecode = sourcecode.ToArray() };
        var lexed = Ronin.Tokens.Symbols.Separator.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
