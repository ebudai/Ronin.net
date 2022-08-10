using Ronin.Parser;
using Ronin.Parser.Grammar;

using List = Ronin.Parser.Grammar.Aggregates.List;

namespace Unit;

public class ListTests : UnitTest
{
    public ListTests() : base("lists") { }

    [Fact(DisplayName = "parse list")]
    public void Parse()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);

        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.NotNull(declaration);
        Assert.Equal("var list as integer", declaration.ToString());

        Assert.IsType<List>(syntax[1]);
        var list = syntax[1] as List;
        Assert.NotNull(list);
        Assert.Empty(list.Expressions);
    }

    [Fact(DisplayName = "parse list literal")]
    public void Literal()
    {
        Assert.True(scope.Expressions.Count > 1);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[1]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);

        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.NotNull(declaration);
        Assert.Equal("var other list =", declaration.ToString());

        Assert.IsType<List>(syntax[1]);
        var list = syntax[1] as List;
        Assert.NotNull(list);
        Assert.Equal(3, list.Expressions.Count);

        syntax = SyntaxProperty.GetValue(list.Expressions[0]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        var literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("1", literal.Value);

        syntax = SyntaxProperty.GetValue(list.Expressions[1]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("2", literal.Value);

        syntax = SyntaxProperty.GetValue(list.Expressions[2]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("5", literal.Value);
    }

    [Fact(DisplayName = "parse nonnumeric list literal")]
    public void NonNumericLiteral()
    {
        Assert.True(scope.Expressions.Count > 2);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[2]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);

        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.NotNull(declaration);
        Assert.Equal("var bank balances =", declaration.ToString());

        Assert.IsType<List>(syntax[1]);
        var list = syntax[1] as List;
        Assert.NotNull(list);
        Assert.Equal(4, list.Expressions.Count);

        syntax = SyntaxProperty.GetValue(list.Expressions[0]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        var literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("$15", literal.Value);

        syntax = SyntaxProperty.GetValue(list.Expressions[1]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("$666", literal.Value);

        syntax = SyntaxProperty.GetValue(list.Expressions[2]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("$27", literal.Value);

        syntax = SyntaxProperty.GetValue(list.Expressions[3]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("$0", literal.Value);
    }

    [Fact(DisplayName = "parse fixed list")]
    public void Fixed()
    {
        Assert.True(scope.Expressions.Count > 3);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[3]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);

        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.NotNull(declaration);
        Assert.Equal("var fixed list as decimal", declaration.ToString());

        Assert.IsType<List>(syntax[1]);
        var list = syntax[1] as List;
        Assert.NotNull(list);
        Assert.NotEmpty(list.Expressions);

        syntax = SyntaxProperty.GetValue(list.Expressions[0]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        var literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("5", literal.Value);
    }

    [Fact(DisplayName = "parse list of lists")]
    public void ListOfLists()
    {
        Assert.True(scope.Expressions.Count > 4);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[4]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(3, syntax.Count);

        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.NotNull(declaration);
        Assert.Equal("var list of lists as maybe", declaration.ToString());

        Assert.IsType<List>(syntax[1]);
        var list = syntax[1] as List;
        Assert.NotNull(list);
        Assert.Empty(list.Expressions);

        Assert.IsType<List>(syntax[2]);
        list = syntax[2] as List;
        Assert.NotNull(list);
        Assert.Empty(list.Expressions);
    }

    [Fact(DisplayName = "parse multidimensional fixed list")]
    public void MultidimensionalFixed()
    {
        Assert.True(scope.Expressions.Count > 5);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[5]) as List<Syntax>;

        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);

        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.NotNull(declaration);
        Assert.Equal("var multidimensional fixed list as date", declaration.ToString());

        Assert.IsType<List>(syntax[1]);
        var list = syntax[1] as List;
        Assert.NotNull(list);
        Assert.Equal(3, list.Expressions.Count);

        syntax = SyntaxProperty.GetValue(list.Expressions[0]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        var literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("5", literal.Value);

        syntax = SyntaxProperty.GetValue(list.Expressions[1]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("1", literal.Value);

        syntax = SyntaxProperty.GetValue(list.Expressions[2]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.NotEmpty(syntax);
        Assert.IsType<Literal>(syntax[0]);
        literal = syntax[0] as Literal;
        Assert.NotNull(literal);
        Assert.Equal("1011", literal.Value);
    }
}
