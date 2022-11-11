using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

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
        Assert.IsType<Reference>(syntax[0]);
        var reference = syntax[0] as Reference;
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Values);
        Ronin.Grammar.Arguments arguments = reference.Values[0];
        Assert.NotNull(arguments);
        Assert.NotEmpty(arguments.Values);
        Name name = arguments.Values[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("test", name.Words[0]);
    }

    /*[Fact(DisplayName = "separated")]
    public void Separated()
    {
        const string declaration = "(test, stuff)";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Reference>(syntax[0]);
        var reference = syntax[0] as Ronin.Grammar.Reference;
        Assert.NotEmpty(reference.Name);
        Assert.True(reference.Name[0].IsT2);
        var @object = reference.Name[0].AsT2;
        Assert.Equal(2, @object.Parameters.Length);

        Assert.NotEmpty(@object.Parameters[0].Name);
        Assert.True(@object.Parameters[0].Name[0].IsT0);
        string name = @object.Parameters[0].Name[0].AsT0;
        Assert.Equal("test", name);

        Assert.NotEmpty(@object.Parameters[1].Name);
        Assert.True(@object.Parameters[1].Name[0].IsT0);
        string stuff = @object.Parameters[1].Name[0].AsT0;
        Assert.Equal("stuff", stuff);
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
        Assert.IsType<Ronin.Grammar.Reference>(syntax[0]);
        var reference = syntax[0] as Ronin.Grammar.Reference;
        Assert.NotEmpty(reference.Name);
        Assert.True(reference.Name[0].IsT2);
        var @object = reference.Name[0].AsT2;
        Assert.Empty(@object.Parameters);

    }*/
}
