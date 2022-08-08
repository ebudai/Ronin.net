using Ronin.Parser;
using Ronin.Parser.Grammar;

using Object = Ronin.Parser.Grammar.Aggregates.Object;

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

        Assert.IsType<Object>(syntax[1]);
        var @object = syntax[1] as Object;
        Assert.NotNull(@object);
        Assert.NotNull(@object.Expressions);
        Assert.Equal(3, @object.Expressions.Count);

        syntax = SyntaxProperty.GetValue(@object.Expressions[0]) as List<Syntax>;
        Assert.NotNull(syntax);

        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        var literal = syntax[0] as Literal;
        Assert.Equal(Primitive.integer, literal.Datatype);
        Assert.Equal("7", literal.Value);

        syntax = SyntaxProperty.GetValue(@object.Expressions[1]) as List<Syntax>;
        Assert.NotNull(syntax);

        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.Equal(Primitive.text, literal.Datatype);
        Assert.Equal("\"12\"", literal.Value);

        syntax = SyntaxProperty.GetValue(@object.Expressions[2]) as List<Syntax>;
        Assert.NotNull(syntax);

        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.Equal(Primitive.money, literal.Datatype);
        Assert.Equal("$15", literal.Value);

    }
}
