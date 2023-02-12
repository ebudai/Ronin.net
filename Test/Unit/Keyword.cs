using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;

namespace Unit;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class keyword
{
    private const string datatype = Datatype.keyword;
    private const string function = Function.keyword;

    [Fact(DisplayName = "datatype")]
    public void Datatypes()
    {
        const string sourcecode = $"{datatype} thing";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("datatype".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "function")]
    public void Functions()
    {
        const string sourcecode = $"{function} thing";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("function".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "variable")]
    public void Variable()
    {
        const string sourcecode = "var thing";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("var".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "constant")]
    public void Constant()
    {
        const string sourcecode = "constant thing";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("constant".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "reactive")]
    public void Reactive()
    {
        const string sourcecode = "reactive thing";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("reactive".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "compiled")]
    public void Compiled()
    {
        const string sourcecode = "compiled thing";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("compiled".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "shared")]
    public void Shared()
    {
        const string sourcecode = "shared thing";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("shared".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "optional")]
    public void Optional()
    {
        const string sourcecode = "optional thing";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("optional".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "persistent")]
    public void Persistent()
    {
        const string sourcecode = "persistent thing";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("persistent".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "part of")]
    public void PartOf()
    {
        const string sourcecode = "part of standard.stuff";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("part of".ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "import")]
    public void Import()
    {
        const string sourcecode = "import git://github.com/ebudai/ronin/libsuperpowers.ronin;";

        Lexer lexer = new(sourcecode);
        var lexed = Keyword.Lex(ref lexer);

        Assert.Equal("import".ToArray(), lexed?.Sourcecode.ToArray());
    }


}
