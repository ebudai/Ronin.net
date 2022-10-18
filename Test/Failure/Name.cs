using Ronin.Compiler;

namespace Failure;

public class Name
{
    [Fact(DisplayName = "not a valid name")]
    public void NotAName()
    {
        const string name = "7stew";

        Lexer lexer = new(name);
        var lexed = Ronin.Token.Word.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "just a symbol")]
    public void Symbol()
    {
        const string name = "(";

        Lexer lexer = new(name);
        var lexed = Ronin.Token.Word.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "just a symbol")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Token.Word.Lex(lexer);

        Assert.Null(lexed);
    }
}
