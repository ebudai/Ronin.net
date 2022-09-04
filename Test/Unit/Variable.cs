using Ronin.Compiler;

namespace Unit;

public class Variable
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string modifier = "var ";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = modifier.ToArray() };
        var lexed = Ronin.Tokens.Modifiers.Variable.Lex(lexer);

        Assert.NotNull(lexed);
    }
}
