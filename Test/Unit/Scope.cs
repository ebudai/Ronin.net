using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Keywords;

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
        var statements = parser.Parse();

        Assert.NotEmpty(statements);
        Temporary value = statements[0];
        Assert.NotNull(value);
        Ronin.Grammar.Aggregates.Scope scope = value;
        Assert.NotNull(scope);
        Assert.NotEmpty(scope.Values);
        
        Ronin.Grammar.Datum datum = scope.Values[0];
        Assert.NotNull(datum);
        
        Assert.IsType<Variable>(datum.Mutability);
        
        Assert.NotNull(datum.Name);
        Assert.NotEmpty(datum.Name.Words);
        Assert.Equal("test", datum.Name.Words[0]);

        Assert.False(datum.Is.Optional);
        Assert.False(datum.Is.Persistent);
        Assert.False(datum.Is.Compiled);
        Assert.False(datum.Is.Shared);

        Assert.NotNull(datum.Initializer);
        value = datum.Initializer;
        Assert.NotNull(value);
        Ronin.Grammar.Scalar scalar = value;
        Assert.NotNull(scalar);
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("56", scalar.Literals[0].ToString());
    }
}
