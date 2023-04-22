using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Date
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "1984-05-04";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), lexed?.sourcecode.ToArray());
    }
}
