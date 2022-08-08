using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class IntegerLiteral : UnitTest
{
    public IntegerLiteral() : base("literals\\integer") { }

    [Fact(DisplayName = "parse integer literal")]
    public void Literal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var normal int =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.integer, literal.Datatype);
        Assert.Equal("92804", literal.Value);
    }

    [Fact(DisplayName = "parse tiny integer literal")]
    public void TinyLiteral()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var tiny integer =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.int8, literal.Datatype);
        Assert.Equal("5i8", literal.Value);
    }

    [Fact(DisplayName = "parse small integer literal")]
    public void SmallLiteral()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[2]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var smallint =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.int16, literal.Datatype);
        Assert.Equal("1000  i16", literal.Value);
    }

    [Fact(DisplayName = "parse large integer literal via suffix")]
    public void LargeSuffixLiteral()
    {
        Assert.True(scope.Expressions.Count > 3);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[3]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var large integer =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.int64, literal.Datatype);
        Assert.Equal("65462168135136i64", literal.Value);
    }

    [Fact(DisplayName = "parse large integer literal via value")]
    public void LargeValueLiteral()
    {
        Assert.True(scope.Expressions.Count > 4);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[4]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var another large integer =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.int64, literal.Datatype);
        Assert.Equal("69843516843518656", literal.Value);
    }

    [Fact(DisplayName = "parse arbitrary integer literal")]
    public void BigintLiteral()
    {
        Assert.True(scope.Expressions.Count > 5);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[5]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var arbitrary integer =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.bigint, literal.Datatype);
        Assert.Equal("32576516816534385321687165416384384381261681681", literal.Value);
    }
}
