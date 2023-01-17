using Ronin.Compiler;
using Ronin.Grammar;

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
        Ronin.Grammar.Datum datum = syntax[0] as Statement;
        Assert.NotNull(datum);
        Assert.NotNull(datum.Datatype);
        Assert.Empty(datum.Datatype.Index.Values);
    }
}
