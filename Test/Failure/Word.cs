using Ronin.Compiler;

namespace Failure;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class word
{
    [Fact(DisplayName = "not a valid name")]
    public void NotAName()
    {
        const string name = "7stew";

        Lexer lexer = new(name);
        var lexed = Ronin.Lexicon.Word.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "just a symbol")]
    public void Symbol()
    {
        const string name = "(";

        Lexer lexer = new(name);
        var lexed = Ronin.Lexicon.Word.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "just a symbol")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Lexicon.Word.Lex(ref lexer);

        Assert.Null(lexed);
    }
}
