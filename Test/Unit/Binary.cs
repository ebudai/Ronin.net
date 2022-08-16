using Ronin.Grammar;
using Ronin.Parser;

namespace Unit;

public class BinaryLiteral : UnitTest
{
    public BinaryLiteral() : base("literals\\binary") { }

    [Fact(DisplayName = "parse binary literal")]
    public void Normal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = scope.Expressions[0].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var normal binary number =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.@byte, literal.Datatype);
        Assert.Equal("101", literal.Value);
    }

    [Fact(DisplayName = "parse separated binary literal")]
    public void Separated()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = scope.Expressions[1].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var binary number with separators =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.bits16, literal.Datatype);
        Assert.Equal("100010100100", literal.Value);
    }

    [Fact(DisplayName = "parse 32-bit binary literal")]
    public void Binary32()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = scope.Expressions[2].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var binary double word =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.bits32, literal.Datatype);
        Assert.Equal("110000000000000011110101", literal.Value);
    }

    [Fact(DisplayName = "parse 64-bit binary literal")]
    public void Binary64()
    {
        Assert.True(scope.Expressions.Count > 3);

        var syntax = scope.Expressions[3].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var binary quad word =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.bits64, literal.Datatype);
        Assert.Equal("1010101010010111001010010010000101111101010101000101", literal.Value);
    }

    [Fact(DisplayName = "parse arbitrary binary literal")]
    public void Bitlist()
    {
        Assert.True(scope.Expressions.Count > 4);

        var syntax = scope.Expressions[4].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var arbitrarily large binary value =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.bitlist, literal.Datatype);
        Assert.Equal("10101010100101110010100100100001011111010101010001001010101010010111001010010010000101111101010101000101010001010100010101000101", literal.Value);
    }
}
