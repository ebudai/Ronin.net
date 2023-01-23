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
        Ronin.Grammar.Assignment assignment = result[0];
        Assert.Equal("x", string.Join(' ', assignment.Name.Words));
        Value value = assignment.Argument;
        Assert.NotNull(value);
        Ronin.Grammar.Scalar scalar = value;
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("17", scalar.Literals[0].ToString());
    }
}
