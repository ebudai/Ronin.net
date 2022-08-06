using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Ronin.Test;

public class Unit
{
    private static readonly PropertyInfo SyntaxProperty = typeof(Expression).GetProperty("Syntax", BindingFlags.Instance | BindingFlags.NonPublic);

    [Fact]
    public void Parse()
    {
        Parser.Parser parser = new(new FileInfo("code.ronin"));

        var scope = parser.ParseScope();

        Assert.NotNull(scope);
        Assert.Equal(3, scope.Expressions.Count);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;
        Assert.NotNull(syntax);
        Assert.Equal(2, syntax.Count);
        
        Assert.IsType<Declaration>(syntax[0]);
        var declaration = syntax[0] as Declaration;
        Assert.Equal("datatype Animal", declaration.ToString());

        Assert.IsType<Scope>(syntax[1]);
        scope = syntax[1] as Scope;
        Assert.Equal(4, scope.Expressions.Count);
    }
}