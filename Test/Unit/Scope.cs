using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Reserved;

namespace Unit;

[Trait("Parser", null)]
public class Scope
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string declaration = "{ var test = 56; }";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Reference reference = syntax[0] as Statement;
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Values);
        Ronin.Grammar.Aggregates.Scope arguments = reference.Values[0];
        Assert.NotNull(arguments);
        Assert.NotEmpty(arguments.Values);
        Ronin.Grammar.Datum datum = arguments.Values[0];
        Assert.NotNull(datum);
        Assert.IsType<Variable>(datum.Mutability);
        Assert.Equal("test", datum.Name.Words[0]);
        var scalar = datum.Initializer.Syntax as Ronin.Grammar.Scalar;
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("56", scalar.Literals[0].ToString());
    }
}
