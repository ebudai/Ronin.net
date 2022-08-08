using Ronin.Parser;
using Ronin.Parser.Grammar;

namespace Unit;

public class HalfLiteral : UnitTest
{
    public HalfLiteral() : base("literals\\half") { }

    [Fact(DisplayName = "parse half precision decimal literal")]
    public void Literal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var half precision =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.dec16, literal.Datatype);
        Assert.Equal("1.0d16", literal.Value);
    }

    [Fact(DisplayName = "parse half precision decimal literal from whole number")]
    public void FromWhole()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var another half =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.dec16, literal.Datatype);
        Assert.Equal("2d16", literal.Value);
    }

    [Fact(DisplayName = "parse half precision decimal literal with separators and a space before suffix")]
    public void SeparatorsAndSpaceBeforeSuffix()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[2]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var with separators and a space =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.dec16, literal.Datatype);
        Assert.Equal("10.1288787954984 d16", literal.Value);
    }
}
