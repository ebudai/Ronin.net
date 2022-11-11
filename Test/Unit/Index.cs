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
        Assert.NotNull(datum?.Datatype?.Index);
        Assert.NotEmpty(datum.Datatype.Index.Values);
        Scalar scalar = datum.Datatype.Index.Values[0];
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("4", scalar.Literals[0].ToString());
    }
}
