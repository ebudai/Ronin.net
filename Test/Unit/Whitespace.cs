using Ronin.Compiler;

namespace Unit;

public class Whitespace
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string source = "    \n\t\r\n\u00A0\u2000\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u205F \u3000";

        Lexer lexer = new() { Sourcecode = source.AsMemory() };
        var whitespace = Ronin.Tokens.Whitespace.Lex(lexer);

        Assert.NotNull(whitespace);
        Assert.Equal(source.ToArray(), whitespace.Sourcecode.ToArray());
    }
}
