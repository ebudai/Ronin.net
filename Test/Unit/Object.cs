using Ronin.Grammar;
using Ronin.Parser;

namespace Unit;

public class ObjectTests : UnitTest
{
    public ObjectTests() : base("objects") { }

    [Fact(DisplayName = "parse object literal")]
    public void Parse()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = scope.Expressions[1].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(3, syntax.Count);

        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.NotNull(declaration);
        Assert.Equal("var", declaration.Name);

        Assert.IsType<Identifier>(syntax[1]);
        var identifier = syntax[1] as Identifier;
        Assert.NotNull(identifier);
        Assert.Equal("bank = run", string.Join(' ', identifier.Names.Values));

        Assert.IsType<Aggregate>(syntax[2]);
        var aggregate = syntax[2] as Aggregate;
        Assert.NotNull(aggregate);
        Assert.NotNull(aggregate.Expressions);
        Assert.Equal(3, aggregate.Expressions.Count);

        syntax = aggregate.Expressions[0].Syntax;
        Assert.NotNull(syntax);

        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        var literal = syntax[0] as Literal;
        Assert.Equal(Scalar.integer, literal.Datatype);
        Assert.Equal("7", literal.Value);

        syntax = aggregate.Expressions[1].Syntax;
        Assert.NotNull(syntax);

        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.Equal(Scalar.text, literal.Datatype);
        Assert.Equal("\"12\"", literal.Value);

        syntax = aggregate.Expressions[2].Syntax;
        Assert.NotNull(syntax);

        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.Equal(Scalar.money, literal.Datatype);
        Assert.Equal("$15", literal.Value);
    }
}
