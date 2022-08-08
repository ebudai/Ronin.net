using Ronin.Parser;
using Ronin.Parser.Grammar;

namespace Unit;

public class CharacterLiteral : UnitTest
{
    public CharacterLiteral() : base("char") { }

    [Fact(DisplayName = "parse character literal")]
    public void Literal()
    {
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

    [Fact(DisplayName = "parse unichar literal")]
    public void UnicharLiteral()
    {
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
