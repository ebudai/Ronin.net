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
        const string declaration = "function test(x => number) { return 7; }";

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
        Assert.Equal("number", datatype.Words[0]);

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

    [Fact(DisplayName = "specifies return datatype")]
    public void Returns()
    {
        const string declaration = "function test(x => text) => optional number { return x as number; }";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.NotEmpty(statements);
        Ronin.Grammar.Function function = statements[0];
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
        Assert.Equal("text", datatype.Words[0]);

        Assert.NotNull(function.Modifiers);
        Assert.True(function.Modifiers.Optional);
        Assert.False(function.Modifiers.Persistent);
        Assert.False(function.Modifiers.Shared);
        Assert.False(function.Modifiers.Compiled);

        Assert.NotNull(function.Returns);
        Assert.Single(function.Returns.Components);
        name = function.Returns.Components[0];
        Assert.Single(name.Words);        
        Assert.Equal("number", name.Words[0]);

        Assert.NotEmpty(function.Body.Values);
        Reference line = function.Body.Values[0];
        Assert.NotNull(line);
        Assert.Single(line.Components);

        Ronin.Grammar.Name @return = line.Components[0];
        Assert.NotNull(@return);
        Assert.Equal(4, @return.Words.Count);
        Assert.Equal("return", @return.Words[0]);
        Assert.Equal("x", @return.Words[1]);
        Assert.Equal("as", @return.Words[2]);
        Assert.Equal("number", @return.Words[3]);
    }
}
