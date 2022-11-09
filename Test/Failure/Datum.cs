using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Reserved;
using Ronin.Lexicon.Symbols;

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

        Assert.Empty(syntax);
    }

    [Fact(DisplayName = $"{Reactive.keyword} before name")]
    public void ReturnsBeforeName()
    {
        const string declaration = $"{Reactive.keyword} {Returns.symbol} 44.3{Semicolon.symbol}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Error>(syntax[0]);
    }

    [Fact(DisplayName = "blank datatype")]
    public void NoDatatypeOrInitializer()
    {
        const string declaration = $"{Variable.keyword} x {Returns.symbol} {Semicolon.symbol}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Error>(syntax[0]);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        const string declaration = $"{Variable.keyword} 555{Semicolon.symbol}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsAssignableFrom<Statement>(syntax[0]);
        var statement = syntax[0] as Statement;
        Assert.NotNull(statement.Reference);
    }    
}
