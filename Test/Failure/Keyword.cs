using Ronin.Compiler;

namespace Failure;

public class Keyword
{
    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Tokens.Keyword.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string unterminated = "function";

        Lexer lexer = new(unterminated);
        var lexed = Ronin.Tokens.Keyword.Lex(lexer);

        Assert.Null(lexed);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }

    [Fact(DisplayName = "not a keyword")]
    public void NotAKeyword()
    {
        const string notkeyword = "not a keyword";

        Lexer lexer = new(notkeyword);
        var lexed = Ronin.Tokens.Keyword.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not really a keyword")]
    public void NotReallyAKeyword()
    {
        const string notkeyword = "returned ";

        Lexer lexer = new(notkeyword);
        var lexed = Ronin.Tokens.Keyword.Lex(lexer);

        Assert.Null(lexed);
    }
}
