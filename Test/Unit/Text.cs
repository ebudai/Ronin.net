using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class TextLiteral
{
    public TextLiteral()
    {
        Parser parser = new(new FileInfo(@"code\literals\text.ronin"));

        scope = parser.ParseScope();

        Assert.NotNull(scope);
    }
    
    private static readonly PropertyInfo SyntaxProperty = typeof(Expression).GetProperty("Syntax", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly Scope scope;

    [Fact(DisplayName = "parse text literal")]
    public void Literal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var normal text =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.text, literal.Datatype);
        Assert.Equal("\"regular text\"", literal.Value);
    }

    [Fact(DisplayName = "parse multiline text literal")]
    public void MultilineLiteral()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var multiline text =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.text, literal.Datatype);
        Assert.Equal("\" this is" + Environment.NewLine + "\tmultiline with whitepsace\"", literal.Value);
    }

    [Fact(DisplayName = "parse text literal with embedded literals")]
    public void LiteralWithEmbeddedLiterals()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[2]) as List<Syntax>;

        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var text with literals inside it =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Primitive.text, literal.Datatype);
        Assert.Equal("\"'c' is a char literal, 0xAAE is hex, 0b1101_0101 is binary\"", literal.Value);
    }
}
