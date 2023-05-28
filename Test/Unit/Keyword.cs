using Ronin.Compiler;
using Ronin.Lexicon.Keywords;

namespace Unit;

[Trait("Lexer", null)]
public class Keyword
{
    private const string datatype = Datatype.keyword;
    private const string function = Function.keyword;
    private const string variable = Variable.keyword;
    private const string constant = Constant.keyword;
    private const string reactive = Reactive.keyword;
    private const string compiled = Compiled.keyword;
    private const string shared = Shared.keyword;
    private const string optional = Optional.keyword;
    private const string persistent = Persistent.keyword;
    private const string partof = PartOf.keyword;
    private const string import = Ronin.Lexicon.Keywords.Import.keyword;
    private const string @foreach = ForEach.keyword;

    [Fact(DisplayName = datatype)]
    public void Datatypes()
    {
        const string sourcecode = $"{datatype} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Datatype;

        Assert.Equal(datatype, keyword?.ToString());
    }

    [Fact(DisplayName = function)]
    public void Functions()
    {
        const string sourcecode = $"{function} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Function;

        Assert.Equal(function, keyword?.ToString());
    }

    [Fact(DisplayName = variable)]
    public void Variables()
    {
        const string sourcecode = $"{variable} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Variable;

        Assert.Equal(variable, keyword?.ToString());
    }

    [Fact(DisplayName = constant)]
    public void Constants()
    {
        const string sourcecode = $"{constant} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Constant;

        Assert.Equal(constant, keyword?.ToString());
    }

    [Fact(DisplayName = reactive)]
    public void Reactives()
    {
        const string sourcecode = $"{reactive} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Reactive;

        Assert.Equal(reactive, keyword?.ToString());
    }

    [Fact(DisplayName = compiled)]
    public void Compileds()
    {
        const string sourcecode = $"{compiled} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Compiled;

        Assert.Equal(compiled, keyword?.ToString());
    }

    [Fact(DisplayName = shared)]
    public void Shareds()
    {
        const string sourcecode = $"{shared} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Shared;

        Assert.Equal(shared, keyword?.ToString());
    }

    [Fact(DisplayName = optional)]
    public void Optionals()
    {
        const string sourcecode = $"{optional} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Optional;

        Assert.Equal(optional, keyword?.ToString());
    }

    [Fact(DisplayName = persistent)]
    public void Persistents()
    {
        const string sourcecode = $"{persistent} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Persistent;

        Assert.Equal(persistent, keyword?.ToString());
    }

    [Fact(DisplayName = partof)]
    public void PartOfs()
    {
        const string sourcecode = $"{partof} standard stuff";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as PartOf;

        Assert.Equal(partof, keyword?.ToString());
    }

    [Fact(DisplayName = import)]
    public void Imports()
    {
        const string sourcecode = "import git://github.com/ebudai/ronin/libsuperpowers.ronin;";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as Ronin.Lexicon.Keywords.Import;

        Assert.Equal(import, keyword?.ToString());
    }

    [Fact(DisplayName = @foreach)]
    public void ForEaches()
    {
        const string sourcecode = "for each thing in all the things { sorgaxulate thing; }";

        Lexer lexer = new(sourcecode);
        var keyword = Ronin.Lexicon.Keyword.Lex(ref lexer) as ForEach;

        Assert.Equal(@foreach, keyword?.ToString());
    }
}
