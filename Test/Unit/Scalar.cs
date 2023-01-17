using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

[Trait("Parser", null)]
public class Scalar
{
    [Fact(DisplayName = "transpile")]
    public void Transpile()
    {
        const string code = "15.4";

        Lexer lexer = new(code);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Reference reference = result[0] as Statement;
        Ronin.Grammar.Scalar scalar = reference.Values[0];
        Assert.NotNull(scalar);
        var transpiled = scalar.ToString();
        Assert.Equal(code, transpiled);
    }
}
