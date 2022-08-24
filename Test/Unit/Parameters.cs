using Ronin.Grammar;
using Ronin.Parser;
using System.Reflection;

namespace Unit;

public class ParameterTests : UnitTest
{
    private static readonly PropertyInfo VariablesProperty = typeof(Parameters).GetProperty("Variables", BindingFlags.Instance | BindingFlags.NonPublic);

    public ParameterTests() : base("parameters") { }

    [Fact(DisplayName = "parse parameters")]
    public void Parse()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = scope.Expressions[0].Syntax;

        Assert.NotNull(syntax);

        Assert.True(syntax.Count > 1);
        Assert.IsType<Parameters>(syntax[1]);
        var parameters = syntax[1] as Parameters;

        var variables = parameters.Data;
        Assert.NotNull(variables);
        Assert.True(variables.Count is 3);

        Assert.True(variables[0].Names.Count is 3);
        Assert.Equal("x", variables[0].Names[0]);
        Assert.Equal("as", variables[0].Names[1]);
        Assert.Equal("integer", variables[0].Names[2]);

        Assert.True(variables[1].Names.Count is 3);
        Assert.Equal("y", variables[1].Names[0]);
        Assert.Equal("as", variables[1].Names[1]);
        Assert.Equal("decimal", variables[1].Names[2]);

        Assert.True(variables[2].Names.Count is 3);
        Assert.Equal("cash", variables[2].Names[0]);
        Assert.Equal("as", variables[2].Names[1]);
        Assert.Equal("money", variables[2].Names[2]);
    }
}
