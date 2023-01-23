using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

[Trait("Parser", null)]
public class Ordinal
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
        Ronin.Grammar.Datum datum = statements[0] as Statement;
        Assert.NotNull(datum?.Datatype?.Ordinal);
        Assert.NotEmpty(datum.Datatype.Ordinal.Values);
        Ronin.Grammar.Scalar scalar = datum.Datatype.Ordinal.Values[0];
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("4", scalar.Literals[0].ToString());
    }
}
