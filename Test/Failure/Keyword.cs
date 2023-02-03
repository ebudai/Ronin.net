using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class keyword
{
    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Keyword.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not a keyword")]
    public void NotAKeyword()
    {
        const string notkeyword = "not a keyword";

        Lexer lexer = new(notkeyword);
        var lexed = Keyword.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not really a keyword")]
    public void NotReallyAKeyword()
    {
        const string notkeyword = "returned ";

        Lexer lexer = new(notkeyword);
        var lexed = Keyword.Lex(lexer);

        Assert.Null(lexed);
    }
}
