using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

[Trait("Parser", null)]
public class Arguments
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string declaration = "(test);";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Temporary temporary = syntax[0];
        Assert.NotNull(temporary);
        Ronin.Grammar.Aggregates.Arguments arguments = temporary;
        Assert.NotNull(arguments);
        Assert.NotEmpty(arguments.Values);
        Reference reference = arguments.Values[0];
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("test", name.Words[0]);
    }

    [Fact(DisplayName = "separated")]
    public void Separated()
    {
        const string declaration = "(test, stuff)";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Temporary temporary = syntax[0];
        Assert.NotNull(temporary);
        Ronin.Grammar.Aggregates.Arguments arguments = temporary;
        Assert.NotNull(arguments);
        Assert.Equal(2, arguments.Values.Count);

        Reference test = arguments.Values[0];
        Assert.NotEmpty(test.Components);
        Ronin.Grammar.Name name = test.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("test", name.Words[0]);

        Reference stuff = arguments.Values[1];
        Assert.NotEmpty(stuff.Components);
        name = stuff.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("stuff", name.Words[0]);
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        const string declaration = "();";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.NotEmpty(statements);
        Temporary value = statements[0];
        Assert.NotNull(value);
        Ronin.Grammar.Aggregates.Arguments arguments = value;
        Assert.NotNull(arguments);
        Assert.Empty(arguments.Values);
    }

    [Fact(DisplayName = "named")]
    public void Named()
    {
        const string declaration = "execute call(1, 2, thing);";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.NotEmpty(statements);
        Reference reference = statements[0];
        Assert.Equal(2, reference.Components.Count);

        Ronin.Grammar.Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.Equal(2, name.Words.Count);
        Assert.Equal("execute", name.Words[0]);
        Assert.Equal("call", name.Words[1]);

        Ronin.Grammar.Aggregates.Arguments arguments = reference.Components[1];
        Assert.NotNull(arguments);
        Assert.NotEmpty(arguments.Values);

        Temporary value = arguments.Values[0];
        Assert.NotNull(value);
        Ronin.Grammar.Scalar scalar = value;
        Assert.NotNull(scalar);
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("1", scalar.Literals[0].ToString());

        value = arguments.Values[1];
        Assert.NotNull(value);
        scalar = value;
        Assert.NotNull(scalar);
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("2", scalar.Literals[0].ToString());

        reference = arguments.Values[2];
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        name = reference.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("thing", name.Words[0]);

    }

}
