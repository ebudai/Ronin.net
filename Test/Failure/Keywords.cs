using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait(nameof(Lexer), null)]
public class Keywords
{
    [Fact(DisplayName = "no data")]
    public void Empty()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not a keyword")]
    public void NotAKeyword()
    {
        const string notkeyword = "not a keyword";

        Lexer lexer = new(notkeyword);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "not really a keyword")]
    public void NotReallyAKeyword()
    {
        const string notkeyword = "returned ";

        Lexer lexer = new(notkeyword);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains keyword")]
    public void ContainsKeyword()
    {
        string[] words =
        {
            $"{Compiled.keyword}ing",
            $"{Constant.keyword}s",
            $"{Ronin.Lexicon.Type.keyword}s",
            $"{ForEach.keyword}ing",
            $"{Function.keyword}s",
            $"{Import.keyword}s",
            $"{Optional.keyword}s",
            $"{PartOf.keyword}fer",
            $"{Reactive.keyword}tion",
            $"{Global.keyword}ding",
            $"{Variable.keyword}rrrr",
            $"{Extend.keyword}ing",
            $"{Hidden.keyword}ning",
            $"{If.keyword}ffff",
            $"{Let.keyword}s party",
            $"{While.keyword}y coyote",
        };

        foreach (var word in words)
        {
            Lexer lexer = new(word);
            var lexed = Keyword.Lex(ref lexer);
            Assert.Null(lexed);
        }
    }
}
