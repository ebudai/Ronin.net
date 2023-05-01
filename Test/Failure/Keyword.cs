using Ronin;
using Ronin.Compiler;

namespace Failure;

[Trait("Lexer", null)]
public class Keyword
{
    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Lexicon.Reserved.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not a keyword")]
    public void NotAKeyword()
    {
        const string notkeyword = "not a keyword";

        Lexer lexer = new(notkeyword);
        var lexed = Ronin.Lexicon.Reserved.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not really a keyword")]
    public void NotReallyAKeyword()
    {
        const string notkeyword = "returned ";

        Lexer lexer = new(notkeyword);
        var lexed = Ronin.Lexicon.Reserved.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains keyword")]
    public void ContainsKeyword()
    {
        string[] words =
        {
            "compileding",
            "constants",
            "datatypes",
            "for eaching",
            "functions",
            "imports",            
            "optionals",
            "part offer",
            "persistentx",
            "reactivetion",
            "sharedding",
            "varrrrr"
        };

        foreach (var word in words)
        {
            Lexer lexer = new(word);
            var lexed = Ronin.Lexicon.Reserved.Lex(ref lexer);
            Assert.Null(lexed);
        }
    }
}
