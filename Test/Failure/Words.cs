using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait(nameof(Lexer), null)]
public class Words
{
    [Fact(DisplayName = "not a valid name")]
    public void NotAName()
    {
        const string name = "7stew";

        Lexer lexer = new(name);
        var lexed = Word.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "just a symbol")]
    public void Symbol()
    {
        const string name = "(";

        Lexer lexer = new(name);
        var lexed = Word.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "just a symbol")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Word.Lex(ref lexer);

        Assert.Null(lexed);
    }
}
