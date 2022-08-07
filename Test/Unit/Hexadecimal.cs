using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class HexadecimalLiteral
{
    private static readonly PropertyInfo SyntaxProperty = typeof(Expression).GetProperty("Syntax", BindingFlags.Instance | BindingFlags.NonPublic);

    private Scope scope;

    public HexadecimalLiteral()
    {
        Parser parser = new(new FileInfo(@"code\literals\hex.ronin"));

        scope = parser.ParseScope();

        Assert.NotNull(scope);
    }

    [Fact(DisplayName = "parse hex literal")]
    public void Normal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var normal hex number =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.integer, literal.Datatype);
        Assert.Equal("75AE2c", literal.Value);
    }

    [Fact(DisplayName = "parse negative hex literal")]
    public void Negative()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var negative hex =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.int8, literal.Datatype);
        Assert.Equal(@"-F", literal.Value);
    }

    [Fact(DisplayName = "parse separated hex literal")]
    public void Separated()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[2]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var hex number with separators =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.int16, literal.Datatype);
        Assert.Equal("4EE3", literal.Value);
    }

    [Fact(DisplayName = "parse negative separated large hex literal")]
    public void Combined()
    {
        Assert.True(scope.Expressions.Count > 3);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[3]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var hex =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.int64, literal.Datatype);
        Assert.Equal("-04cDEADBEEF3000", literal.Value);
    }
}
