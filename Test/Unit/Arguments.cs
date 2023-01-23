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
        Reference reference = syntax[0];
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Aggregates.Arguments arguments = reference.Components[0];
        Assert.NotNull(arguments);
        Assert.NotEmpty(arguments.Values);
        reference = arguments.Values[0];
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
        Reference reference = syntax[0];
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Aggregates.Arguments arguments = reference.Components[0];
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
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Reference reference = syntax[0] as Statement;
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Aggregates.Arguments @object = reference.Components[0];
        Assert.NotNull(@object);
        Assert.Empty(@object.Values);
    }
}
