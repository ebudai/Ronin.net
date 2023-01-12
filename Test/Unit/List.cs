using Ronin.Compiler;

namespace Unit;

[Trait("Parser", null)]
public class List
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string declaration = "var x => integer[];";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Datum.Declaration>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Datum.Declaration;
        Assert.NotNull(datum);
        Assert.NotNull(datum.Datatype);
        Assert.Empty(datum.Datatype.Index.Values);
    }
}
