using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class name
{
    [Fact(DisplayName = "symbols")]
    public void Symbols()
    {
        const string code = "name+things;";

        Lexer lexer = new(code);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Reference reference = result[0];
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        Name name = reference.Components[0];
        Assert.Equal(3, name.Words.Count);
        Assert.Equal("name", name.Words[0]);
        Assert.Equal("+", name.Words[1]);
        Assert.Equal("things", name.Words[2]);
    }
}
