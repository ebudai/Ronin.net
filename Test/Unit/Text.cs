using Ronin.Grammar;
using Ronin.Parser;

namespace Unit;

public class TextLiteral : UnitTest
{
    public TextLiteral() : base("literals\\text") { }

    [Fact(DisplayName = "parse text literal")]
    public void Literal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = scope.Expressions[0].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var normal text =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.text, literal.Datatype);
        Assert.Equal("\"regular text\"", literal.Value);
    }

    [Fact(DisplayName = "parse multiline text literal")]
    public void MultilineLiteral()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = scope.Expressions[1].Syntax;

        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var multiline text =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.text, literal.Datatype);
        Assert.Equal("\" this is" + Environment.NewLine + "\tmultiline with whitepsace\"", literal.Value);
    }

    [Fact(DisplayName = "parse text literal with embedded literals")]
    public void LiteralWithEmbeddedLiterals()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = scope.Expressions[2].Syntax;

        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var text with literals inside it =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.text, literal.Datatype);
        Assert.Equal("\"'c' is a char literal, 0xAAE is hex, 0b1101_0101 is binary\"", literal.Value);
    }
}
