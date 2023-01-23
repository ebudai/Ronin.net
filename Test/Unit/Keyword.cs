namespace Unit;

[Trait("Lexer", null)]
public class Keyword
{
    private const string datatype = Ronin.Lexicon.Keywords.Datatype.keyword;
    private const string function = Ronin.Lexicon.Keywords.Function.keyword;

    [Fact(DisplayName = "datatype")]
    public void Datatype()
    {
        const string sourcecode = $"{datatype} thing";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("datatype".ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "function")]
    public void Function()
    {
        const string sourcecode = $"{function} thing";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("function".ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "variable")]
    public void Variable()
    {
        const string sourcecode = "var thing";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("var".ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "constant")]
    public void Constant()
    {
        const string sourcecode = "constant thing";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("constant".ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "reactive")]
    public void Reactive()
    {
        const string sourcecode = "reactive thing";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("reactive".ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "compiled")]
    public void Compiled()
    {
        const string sourcecode = "compiled constant thing";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("compiled".ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "shared")]
    public void Shared()
    {
        const string sourcecode = "shared thing";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("shared".ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "optional")]
    public void Optional()
    {
        const string sourcecode = "optional thing";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("optional".ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "part of")]
    public void PartOf()
    {
        const string sourcecode = "part of standard.stuff";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("part of".ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "import")]
    public void Import()
    {
        const string sourcecode = "import git://github.com/ebudai/ronin/libsuperpowers.ronin;";

        Ronin.Compiler.Lexer lexer = new(sourcecode);
        var lexed = Ronin.Lexicon.Keyword.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal("import".ToArray(), lexed.Sourcecode.ToArray());
    }


}
