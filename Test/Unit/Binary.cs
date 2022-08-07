using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class BinaryLiteral
{
    public BinaryLiteral()
    {
        Parser parser = new(new FileInfo(@"code\literals\binary.ronin"));

        scope = parser.ParseScope();

        Assert.NotNull(scope);
    }

    private static readonly PropertyInfo SyntaxProperty = typeof(Expression).GetProperty("Syntax", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly Scope scope;

    [Fact(DisplayName = "parse binary literal")]
    public void Normal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var normal binary number =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.@byte, literal.Datatype);
        Assert.Equal("101", literal.Value);
    }

    [Fact(DisplayName = "parse separated binary literal")]
    public void Separated()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var binary number with separators =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.bits16, literal.Datatype);
        Assert.Equal("100010100100", literal.Value);
    }

    [Fact(DisplayName = "parse 32-bit binary literal")]
    public void Binary32()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[2]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var binary double word =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.bits32, literal.Datatype);
        Assert.Equal("110000000000000011110101", literal.Value);
    }

    [Fact(DisplayName = "parse 64-bit binary literal")]
    public void Binary64()
    {
        Assert.True(scope.Expressions.Count > 3);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[3]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var binary quad word =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.bits64, literal.Datatype);
        Assert.Equal("1010101010010111001010010010000101111101010101000101", literal.Value);
    }

    [Fact(DisplayName = "parse arbitrary binary literal")]
    public void Bitlist()
    {
        Assert.True(scope.Expressions.Count > 4);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[4]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var arbitrarily large binary value =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.bitlist, literal.Datatype);
        Assert.Equal("10101010100101110010100100100001011111010101010001001010101010010111001010010010000101111101010101000101010001010100010101000101", literal.Value);
    }
}
