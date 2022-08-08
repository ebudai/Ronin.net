using Ronin.Parser;
using Ronin.Parser.Grammar;
using System.Reflection;

namespace Unit;

public class ParameterTests : UnitTest
{
    private static readonly PropertyInfo VariablesProperty = typeof(Parameters).GetProperty("Variables", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo NamesField = typeof(Identifier).GetField("names", BindingFlags.Instance | BindingFlags.NonPublic);

    public ParameterTests() : base("parameters") { }

    [Fact(DisplayName = "parse parameters")]
    public void Parse()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = SyntaxProperty.GetValue(scope.Expressions[0]) as List<Syntax>;

        Assert.NotNull(syntax);

        Assert.True(syntax.Count > 1);
        Assert.IsType<Parameters>(syntax[1]);
        var parameters = syntax[1] as Parameters;

        var variables = VariablesProperty.GetValue(parameters) as List<Identifier>;
        Assert.NotNull(variables);
        Assert.True(variables.Count is 3);

        var names = NamesField.GetValue(variables[0]) as List<string>;
        Assert.NotNull(names);
        Assert.True(names.Count is 3);
        Assert.Equal("x", names[0]);
        Assert.Equal("as", names[1]);
        Assert.Equal("integer", names[2]);

        names = NamesField.GetValue(variables[1]) as List<string>;
        Assert.NotNull(names);
        Assert.True(names.Count is 3);
        Assert.Equal("y", names[0]);
        Assert.Equal("as", names[1]);
        Assert.Equal("decimal", names[2]);

        names = NamesField.GetValue(variables[2]) as List<string>;
        Assert.NotNull(names);
        Assert.True(names.Count is 3);
        Assert.Equal("cash", names[0]);
        Assert.Equal("as", names[1]);
        Assert.Equal("money", names[2]);
    }
}
