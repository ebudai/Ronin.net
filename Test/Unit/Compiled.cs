using Ronin.Compiler;

namespace Unit;

public class Compiled
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string modifier = "compiled ";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = modifier.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Compiled.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
