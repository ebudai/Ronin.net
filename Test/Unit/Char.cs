using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class CharacterLiteral
{
    private static readonly PropertyInfo SyntaxProperty = typeof(Expression).GetProperty("Syntax", BindingFlags.Instance | BindingFlags.NonPublic);

    [Fact(DisplayName = "Parse character literal")]
    public void Literal()
    {
        Parser parser = new(new FileInfo(@"code\literals\char.ronin"));

        var scope = parser.ParseScope();

        Assert.NotNull(scope);

        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var regular char =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.character, literal.Datatype);
        Assert.Equal("'c'", literal.Value);
    }

    [Fact(DisplayName = "Parse unichar literal")]
    public void UnicharLiteral()
    {
        Parser parser = new(new FileInfo(@"code\literals\char.ronin"));

        var scope = parser.ParseScope();

        Assert.NotNull(scope);

        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var unichar =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.character, literal.Datatype);
        Assert.Equal(@"'\u05E4'", literal.Value);
    }
}
