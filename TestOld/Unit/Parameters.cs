using Ronin.Grammar;
using Ronin.Parser;
using System.Reflection;

namespace Unit;

public class ParameterTests : UnitTest
{
    public ParameterTests() : base("parameters") { }

    [Fact(DisplayName = "parse parameters")]
    public void Parse()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = scope.Expressions[0].Syntax;

        Assert.NotNull(syntax);

        Assert.True(syntax.Count > 1);
        Assert.IsType<Parameters>(syntax[2]);
        var parameters = syntax[2] as Parameters;

        var variables = parameters.Data;
        Assert.NotNull(variables);
        Assert.True(variables.Count is 3);

        Assert.True(variables[0].Names.Count is 1);
        Assert.Equal("x as integer", variables[0].Names[0]);

        Assert.True(variables[1].Names.Count is 1);
        Assert.Equal("y as decimal", variables[1].Names[0]);

        Assert.True(variables[2].Names.Count is 1);
        Assert.Equal("cash as money", variables[2].Names[0]);
    }
}
