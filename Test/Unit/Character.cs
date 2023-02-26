using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Character
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "'c'";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), lexed?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "unicode")]
    public void Unicode()
    {
        const string literal = @"'\u44A2'";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), lexed?.sourcecode.ToArray());
    }
}
