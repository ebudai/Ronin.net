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
        Assert.NotNull(assignment);
        Assert.Single(assignment.Reference.Components);
        Ronin.Grammar.Name name = assignment.Reference.Components[0];
        Assert.NotEmpty(name.Words);
        Assert.Equal("x", string.Join(' ', name.Words));
        Temporary value = assignment.Value;
        Assert.NotNull(value);
        Ronin.Grammar.Scalar scalar = value;
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("17", scalar.Literals[0].ToString());
    }
}
