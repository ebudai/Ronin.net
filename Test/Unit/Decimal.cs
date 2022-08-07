using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class DecimalLiteral
{
    public DecimalLiteral()
    {
        Parser parser = new(new FileInfo(@"code\literals\decimal.ronin"));

        scope = parser.ParseScope();

        Assert.NotNull(scope);
    }

    private static readonly PropertyInfo SyntaxProperty = typeof(Expression).GetProperty("Syntax", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly Scope scope;

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
