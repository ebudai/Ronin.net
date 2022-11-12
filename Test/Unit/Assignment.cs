using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

[Trait("Parser", null)]
public class Assignment
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string line = "x = 17;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Assert.IsType<Ronin.Grammar.Assignment>(result[0]);
        var assignment = result[0] as Ronin.Grammar.Assignment;
        Assert.Equal("x", string.Join(' ', assignment.Name.Words));
        Scalar value = assignment.Value;
        Assert.NotNull(value);
        Assert.NotEmpty(value.Literals);
        Assert.Equal("17", value.Literals[0].ToString());
    }
}
