using Ronin.Compiler;

namespace Unit;

public class Datatype
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string modifier = "datatype ";

        Lexer lexer = new() { Sourcecode = modifier.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Datatype.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
