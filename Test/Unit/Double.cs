using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class DoubleLiteral
{
    public DoubleLiteral()
    {
        Parser parser = new(new FileInfo(@"code\literals\double.ronin"));

        scope = parser.ParseScope();

        Assert.NotNull(scope);
    }

    private static readonly PropertyInfo SyntaxProperty = typeof(Expression).GetProperty("Syntax", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly Scope scope;

    [Fact(DisplayName = "parse double precision decimal literal")]
    public void Literal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var double precision =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.dec64, literal.Datatype);
        Assert.Equal("8.22d64", literal.Value);
    }

    [Fact(DisplayName = "parse double precision decimal literal from whole number")]
    public void FromWhole()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var another double =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.dec64, literal.Datatype);
        Assert.Equal("55d64", literal.Value);
    }

    [Fact(DisplayName = "parse double precision decimal literal with space before suffix")]
    public void SpaceBeforeSuffix()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[2]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var last double =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.dec64, literal.Datatype);
        Assert.Equal("12320.695134781351680 d64", literal.Value);
    }
}
