using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Reserved;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Datum
{
    private const string reactive = Reactive.keyword;
    private const string returns = Returns.symbol;
    private const string end = Terminal.symbol;
    private const string var = Variable.keyword;

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
        const string declaration = $"{reactive} {returns} 44.3{end}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsAssignableFrom<Error>(syntax[0]);
    }

    [Fact(DisplayName = "blank datatype")]
    public void NoDatatypeOrInitializer()
    {
        const string declaration = $"{var} x {returns} {end}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsAssignableFrom<Error>(syntax[0]);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        const string declaration = $"{var} 555{end}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Reference reference = syntax[0] as Statement;
        Assert.NotNull(reference);
    }

    [Fact(DisplayName = "missing datatype and initializer")]
    public void MissingDatatypeAndInitializer()
    {
        const string declaration = $"{var} x{end}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<UnspecifiedDatatypeError>(syntax[0]);
    }
}
