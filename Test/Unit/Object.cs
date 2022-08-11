using Ronin.Parser;
using Ronin.Parser.Grammar;

namespace Unit;

public class ObjectTests : UnitTest
{
    public ObjectTests() : base("objects") { }

    [Fact(DisplayName = "parse object literal")]
    public void Parse()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);

        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.NotNull(declaration);
        Assert.Equal("var bank = run", declaration.ToString());

        Assert.IsType<Aggregate>(syntax[1]);
        var aggregate = syntax[1] as Aggregate;
        Assert.NotNull(aggregate);
        Assert.NotNull(aggregate.Expressions);
        Assert.Equal(3, aggregate.Expressions.Count);

        syntax = SyntaxProperty.GetValue(aggregate.Expressions[0]) as List<Syntax>;
        Assert.NotNull(syntax);

        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        var literal = syntax[0] as Literal;
        Assert.Equal(Primitive.integer, literal.Datatype);
        Assert.Equal("7", literal.Value);

        syntax = SyntaxProperty.GetValue(aggregate.Expressions[1]) as List<Syntax>;
        Assert.NotNull(syntax);

        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.Equal(Primitive.text, literal.Datatype);
        Assert.Equal("\"12\"", literal.Value);

        syntax = SyntaxProperty.GetValue(aggregate.Expressions[2]) as List<Syntax>;
        Assert.NotNull(syntax);

        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.Equal(Primitive.money, literal.Datatype);
        Assert.Equal("$15", literal.Value);

    }
}
