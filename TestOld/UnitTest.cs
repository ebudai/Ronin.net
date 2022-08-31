using Ronin.Grammar;
using Ronin.Parser;

namespace Unit;

public class UnitTest
{
    protected internal UnitTest(string name)
    {
        Context context = new(new FileInfo(@$"code\{name}.ronin"));

        scope = ScopeParser.Parse(context);

        Assert.NotNull(scope);
    }

    internal readonly Scope scope;
}

public class LiteralUnitTest : UnitTest
{
    public LiteralUnitTest(string name) : base(name)
    {

    }

    public void Test(int index, string identifier, string value, string type)
    {
        Assert.True(scope.Expressions.Count > index);

        var syntax = scope.Expressions[index].Syntax;

        Assert.NotNull(syntax);
        Assert.Equal(3, syntax.Count);

        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("var", string.Join(' ', declaration.Modifiers.Select(name => name.Trim())));

        Assert.IsType<Identifier>(syntax[1]);
        var name = syntax[1] as Identifier;
        Assert.Equal(identifier, string.Join(' ', name.Names.Values));

        Assert.IsType<Literal>(syntax[2]);
        var literal = syntax[2] as Literal;
        Assert.Equal(type, literal.Datatype);
        Assert.Equal(value, literal.Value);
    }
}