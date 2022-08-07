using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class HexLiteral
{
    public HexLiteral()
    {
        Parser parser = new(new FileInfo(@"code\literals\hex.ronin"));

        scope = parser.ParseScope();

        Assert.NotNull(scope);
    }

    private static readonly PropertyInfo SyntaxProperty = typeof(Expression).GetProperty("Syntax", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly Scope scope;

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
        Assert.Equal(Primitive.bits32, literal.Datatype);
        Assert.Equal("75AE2c", literal.Value);
    }

    [Fact(DisplayName = "parse separated hex literal")]
    public void Separated()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var hex number with separators =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.bits16, literal.Datatype);
        Assert.Equal("4EE3", literal.Value);
    }

    [Fact(DisplayName = "parse small hex literal")]
    public void SmallHex()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[2]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var small hex =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.@byte, literal.Datatype);
        Assert.Equal("F", literal.Value);
    }

    [Fact(DisplayName = "parse big hex literal")]
    public void BigHex()
    {
        Assert.True(scope.Expressions.Count > 3);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[3]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var big hex number =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.bits64, literal.Datatype);
        Assert.Equal("4cDEADBEEF3000", literal.Value);
    }

    [Fact(DisplayName = "parse arbitrary hex literal")]
    public void Bitlist()
    {
        Assert.True(scope.Expressions.Count > 4);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[4]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var arbitrary hex number =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.bitlist, literal.Datatype);
        Assert.Equal("1AAAAAAAAEeEEeEBBBBBBBBBdddDD00111666666667", literal.Value);
    }
}
