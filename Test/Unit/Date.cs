using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class date
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "1984-05-04";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Equal(literal.ToArray(), lexed?.Sourcecode.ToArray());
    }
}
