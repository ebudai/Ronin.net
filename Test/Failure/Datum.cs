using Ronin.Compiler;
using Ronin.Grammar;

namespace Failure;

public class Datum
{
    [Fact(DisplayName = "comments and whitespace")]
    public void CommentsAndWhitespace()
    {
        const string sourcecode = "  /* some comments */   ";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.IsType<Trivium>(syntax[0]);
    }

    [Fact(DisplayName = "returns before name")]
    public void ReturnsBeforeName()
    {
        const string declaration = "reactive => 44.3;";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Expected<Ronin.Token.Word>>(syntax[0]);
    }

    [Fact(DisplayName = "blank datatype")]
    public void NoDatatypeOrInitializer()
    {
        const string declaration = "var x => ;";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsAssignableFrom<Unexpected>(syntax[0]);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        const string declaration = "var 555;";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsAssignableFrom<Reference>(syntax[0]);
    }
}
