using Ronin.Parser;
using Ronin.Parser.Grammar;

namespace Unit;

public class DecimalLiteral : UnitTest
{
    public DecimalLiteral() : base("literals\\decimal") { }

    [Fact(DisplayName = "parse default precision decimal literal")]
    public void Literal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var decimal literal =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.@decimal, literal.Datatype);
        Assert.Equal("107.2", literal.Value);
    }
}
