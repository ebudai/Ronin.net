using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

public class Index
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string line = "var test => integer[4];";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.NotEmpty(statements);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(statements[0]);
        var datum = statements[0] as Ronin.Grammar.Declaration.Datum;
        Assert.NotNull(datum);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Values);
        Name name = datum.Datatype.Values[0];
        Assert.NotNull(name);
    }
}
