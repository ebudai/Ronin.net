using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Keywords
{
    private const string type = Ronin.Lexicon.Type.keyword;
    private const string function = Function.keyword;
    private const string variable = Variable.keyword;
    private const string constant = Constant.keyword;
    private const string reactive = Reactive.keyword;
    private const string compiled = Compiled.keyword;
    private const string shared = Global.keyword;
    private const string optional = Optional.keyword;
    private const string partof = PartOf.keyword;
    private const string import = Import.keyword;
    private const string @foreach = Iterate.keyword;
    private const string extends = Extend.keyword;
    private const string @if = If.keyword;
    private const string let = Let.keyword;
    private const string @while = While.keyword;
    private const string when = When.keyword;
    private const string changing = Changing.keyword;
    private const string hidden = Hidden.keyword;

    [Fact(DisplayName = type)]
    public void TypeKeyword()
    {
        const string sourcecode = $"{type} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Ronin.Lexicon.Type;

        Assert.Equal(type, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = function)]
    public void FunctionKeyword()
    {
        const string sourcecode = $"{function} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Function;

        Assert.Equal(function, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = variable)]
    public void VariableKeyword()
    {
        const string sourcecode = $"{variable} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Variable;

        Assert.Equal(variable, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = constant)]
    public void ConstantKeyword()
    {
        const string sourcecode = $"{constant} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Constant;

        Assert.Equal(constant, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = reactive)]
    public void ReactiveKeyword()
    {
        const string sourcecode = $"{reactive} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Reactive;

        Assert.Equal(reactive, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = compiled)]
    public void CompiledKeyword()
    {
        const string sourcecode = $"{compiled} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Compiled;

        Assert.Equal(compiled, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = shared)]
    public void SharedKeyword()
    {
        const string sourcecode = $"{shared} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Global;

        Assert.Equal(shared, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = optional)]
    public void OptionalKeyword()
    {
        const string sourcecode = $"{optional} thing";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Optional;

        Assert.Equal(optional, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = partof)]
    public void PartOfKeyword()
    {
        const string sourcecode = $"{partof} standard stuff";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as PartOf;

        Assert.Equal(partof, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = import)]
    public void ImportKeyword()
    {
        const string sourcecode = "import git://github.com/ebudai/ronin/libsuperpowers.ronin;";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Import;

        Assert.Equal(import, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = @foreach)]
    public void ForEachKeyword()
    {
        const string sourcecode = "iterate all the things => thing { sorgaxulate thing; }";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Iterate;

        Assert.Equal(@foreach, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = extends)]
    public void ExtendsKeyword()
    {
        const string sourcecode = "extend whatch'ma call it { var x => something; }";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Extend;

        Assert.Equal(extends, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = @if)]
    public void IfKeyword()
    {
        const string sourcecode = "if x > 3 { return something; }";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as If;

        Assert.Equal(@if, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = let)]
    public void LetKeyword()
    {
        const string sourcecode = "let x = 3;";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Let;

        Assert.Equal(let, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = @while)]
    public void WhileKeyword()
    {
        const string sourcecode = "while x < 3 { y += 3; }";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as While;

        Assert.Equal(@while, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = when)]
    public void WhenKeyword()
    {
        const string sourcecode = "when x < 3 { y += 3; }";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as When;

        Assert.Equal(when, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = $"{when} {changing}")]
    public void WhenChangingKeyword()
    {
        const string sourcecode = "when changing x { y += 3; }";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as When;

        Assert.Equal(when, keyword?.Memory.ToString());
    }

    [Fact(DisplayName = hidden)]
    public void HiddenKeyword()
    {
        const string sourcecode = "hidden var x => number;";

        Lexer lexer = new(sourcecode);
        var keyword = Keyword.Lex(ref lexer) as Hidden;

        Assert.Equal(hidden, keyword?.Memory.ToString());
    }
}
