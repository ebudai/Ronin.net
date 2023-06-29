using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;

namespace Unit;

[Trait("Lexer", null)]
public class Keywords
{
    private const string datatype = Ronin.Lexicon.Keywords.Datatype.keyword;
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
    private const string extends = Extends.keyword;

    [Fact(DisplayName = datatype)]
    public void DatatypeKeyword()
    {
        const string sourcecode = $"{datatype} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Ronin.Lexicon.Keywords.Datatype;

        Assert.Equal(datatype, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = function)]
    public void FunctionKeyword()
    {
        const string sourcecode = $"{function} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Function;

        Assert.Equal(function, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = variable)]
    public void VariableKeyword()
    {
        const string sourcecode = $"{variable} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Variable;

        Assert.Equal(variable, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = constant)]
    public void ConstantKeyword()
    {
        const string sourcecode = $"{constant} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Constant;

        Assert.Equal(constant, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = reactive)]
    public void ReactiveKeyword()
    {
        const string sourcecode = $"{reactive} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Reactive;

        Assert.Equal(reactive, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = compiled)]
    public void CompiledKeyword()
    {
        const string sourcecode = $"{compiled} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Compiled;

        Assert.Equal(compiled, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = shared)]
    public void SharedKeyword()
    {
        const string sourcecode = $"{shared} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Shared;

        Assert.Equal(shared, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = optional)]
    public void OptionalKeyword()
    {
        const string sourcecode = $"{optional} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Optional;

        Assert.Equal(optional, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = persistent)]
    public void PersistentKeyword()
    {
        const string sourcecode = $"{persistent} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Persistent;

        Assert.Equal(persistent, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = partof)]
    public void PartOfKeyword()
    {
        const string sourcecode = $"{partof} standard stuff";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as PartOf;

        Assert.Equal(partof, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = import)]
    public void ImportKeyword()
    {
        const string sourcecode = "import git://github.com/ebudai/ronin/libsuperpowers.ronin;";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Ronin.Lexicon.Keywords.Import;

        Assert.Equal(import, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = @foreach)]
    public void ForEachKeyword()
    {
        const string sourcecode = "for each thing in all the things { sorgaxulate thing; }";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as ForEach;

        Assert.Equal(@foreach, keyword?.Memory.ToArray());
    }

    [Fact(DisplayName = extends)]
    public void ExtendsKeyword()
    {
        const string sourcecode = "extends datatype whatch'ma call it { var x => something; }";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Extends;

        Assert.Equal(extends, keyword?.Memory.ToArray());
    }
}
