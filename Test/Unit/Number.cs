using Ronin.Grammar;
using Ronin.Parser;

namespace Unit;

public class NumberLiteral : UnitTest
{
    public NumberLiteral() : base("literals\\number") { }

    [Fact(DisplayName = "parse number literal")]
    public void Literal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = scope.Expressions[0].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var decimal literal =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.number, literal.Datatype);
        Assert.Equal("107.2", literal.Value);
    }
}
