using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class whitespace
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string source = "    \n\t\r\n\u00A0\u2000\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u205F \u3000";

        Ronin.Compiler.Lexer lexer = new(source);
        var whitespace = Whitespace.Lex(lexer);

        Assert.NotNull(whitespace);
        Assert.Equal(source.ToArray(), whitespace.Sourcecode.ToArray());
    }
}
