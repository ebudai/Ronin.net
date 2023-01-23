using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;

namespace Unit;

[Trait("Parser", null)]
public class Function
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string declaration = "function test(x => integer) { return 7; }";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Ronin.Grammar.Function function = result[0];
        Assert.NotNull(function);
        Assert.Equal(2, function.Identifier.Components.Count);

        Ronin.Grammar.Name name = function.Identifier.Components[0];
        Assert.NotNull(name);
        Assert.Equal("test", string.Join(' ', name.Words));

        Parameters parameters = function.Identifier.Components[1];
        Assert.NotNull(parameters);
        Assert.NotEmpty(parameters.Values);
        var parameter = parameters.Values[0];
        Assert.NotEmpty(parameter.Name.Words);
        Assert.Equal("x", parameter.Name.Words[0]);
        
        Assert.NotEmpty(parameter.Datatype.Components);
        Ronin.Grammar.Name datatype = parameter.Datatype.Components[0];
        Assert.NotEmpty(datatype.Words);
        Assert.Equal("integer", datatype.Words[0]);

        Assert.NotEmpty(function.Body.Values);
        Reference line = function.Body.Values[0];
        Assert.NotNull(line);
        Assert.Equal(2, line.Components.Count);

        Ronin.Grammar.Name @return = line.Components[0];
        Assert.NotNull(@return);
        Assert.NotEmpty(@return.Words);
        Assert.Equal("return", @return.Words[0]);

        Ronin.Grammar.Scalar scalar = line.Components[1];
        Assert.NotNull(scalar);
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("7", scalar.Literals[0].ToString());
        
    }
}
