using Ronin.Compiler;

namespace Failure;

[Trait("Lexer", null)]
public class Keyword
{
    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Lexicon.Keyword.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not a keyword")]
    public void NotAKeyword()
    {
        const string notkeyword = "not a keyword";

        Lexer lexer = new(notkeyword);
        var lexed = Ronin.Lexicon.Keyword.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not really a keyword")]
    public void NotReallyAKeyword()
    {
        const string notkeyword = "returned ";

        Lexer lexer = new(notkeyword);
        var lexed = Ronin.Lexicon.Keyword.Lex(ref lexer);

        Assert.Null(lexed);
    }
}
