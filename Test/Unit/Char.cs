using Ronin.Grammar;
using Ronin.Parser;

namespace Unit;

public class CharacterLiteral : UnitTest
{
    public CharacterLiteral() : base("literals\\char") { }

    [Fact(DisplayName = "parse character literal")]
    public void Literal()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = scope.Expressions[0].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var regular char =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.character, literal.Datatype);
        Assert.Equal("'c'", literal.Value);
    }

    [Fact(DisplayName = "parse unichar literal")]
    public void UnicharLiteral()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = scope.Expressions[1].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var unichar =", declaration.ToString());
        Assert.IsType<Literal>(syntax[1]);
        var literal = syntax[1] as Literal;
        Assert.Equal(Scalar.character, literal.Datatype);
        Assert.Equal(@"'\u05E4'", literal.Value);
    }
}
