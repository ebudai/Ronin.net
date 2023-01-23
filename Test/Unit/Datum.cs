using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
public class Datum
{
    private const string var = Variable.keyword;
    private const string returns = Returns.symbol;
    private const string end = Terminal.symbol;
    private const string reactive = Reactive.keyword;
    private const string compiled = Compiled.keyword;
    private const string persistent = Persistent.keyword;
    private const string constant = Constant.keyword;
    private const string shared = Shared.keyword;
    private const string optional = Optional.keyword;
    private const string import = Import.keyword;
    private const string equals = Assign.symbol;

    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        const string declaration = $"{var} my variable {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Variable);
        Assert.Equal("my variable", string.Join(' ', datum.Name.Words));
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        var datatype = name.Words[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{reactive}")]
    public void ReactiveDatatype()
    {
        const string declaration = $"{reactive} x {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Reactive);
    }

    [Fact(DisplayName = $"{compiled}")]
    public void CompiledDatatype()
    {
        const string declaration = $"{var} x {returns} {compiled} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Compiled);
    }

    [Fact(DisplayName = $"{persistent}")]
    public void PersistentDatatype()
    {
        const string declaration = $"{constant} x {returns} {persistent} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Constant);
        Assert.True(datum.Is.Persistent);
    }

    [Fact(DisplayName = $"{shared}")]
    public void SharedDatatype()
    {
        const string declaration = $"{var} x {returns} {shared} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Shared);
    }

    [Fact(DisplayName = $"{optional}")]
    public void OptionalDatatype()
    {
        const string declaration = $"{reactive} x {returns} {optional} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Optional);
    }

    [Fact(DisplayName = $"{reactive} twice")]
    public void ReactiveTwiceIsOk()
    {
        const string declaration = $"{reactive} {reactive} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Reactive);
        Assert.Equal($"{reactive} thing", string.Join(' ', datum.Name.Words));        
    }

    [Fact(DisplayName = $"{constant} twice")]
    public void ConstantTwiceIsOk()
    {
        const string declaration = $"{constant} {constant} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Constant);
        Assert.Equal($"{constant} thing", string.Join(' ', datum.Name.Words));
    }

    [Fact(DisplayName = $"{var} twice")]
    public void VarTwiceIsOk()
    {
        const string declaration = $"{var} {var} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.False(datum.Mutability is Constant);
        Assert.Equal($"{var} thing", string.Join(' ', datum.Name.Words));
    }

    [Fact(DisplayName = $"{compiled} twice")]
    public void CompiledTwiceIsOk()
    {
        const string declaration = $"{var} thing {returns} {compiled} {compiled} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Compiled);
        var name = string.Join(' ', datum.Datatype.Components.Select(value => (Ronin.Grammar.Name)value).SelectMany(name => name.Words));
        Assert.Equal($"{compiled} integer", name);
    }

    [Fact(DisplayName = $"{persistent} twice")]
    public void PersistentTwiceIsOk()
    {
        const string declaration = $"{var} thing {returns} {persistent} {persistent} integer{end}";

        var datum = Compile/* bugaboo */(declaration);

        Assert.True(datum.Is.Persistent);
        var name = string.Join(' ', datum.Datatype.Components.Select(value => (Ronin.Grammar.Name)value).SelectMany(name => name.Words));
        Assert.Equal($"{persistent} integer", name);
    }

    [Fact(DisplayName = $"{optional} twice")]
    public void OptionalTwiceIsOk()
    {
        const string declaration = $"{var} thing {returns} {optional} {optional} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Optional);
        var name = string.Join(' ', datum.Datatype.Components.Select(value => (Ronin.Grammar.Name)value).SelectMany(name => name.Words));
        Assert.Equal($"{optional} integer", name);
    }

    [Fact(DisplayName = $"{shared} twice")]
    public void SharedTwiceIsOk()
    {
        const string declaration = $"{var} thing {returns} {shared} {shared} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Shared);
        var name = string.Join(' ', datum.Datatype.Components.Select(value => (Ronin.Grammar.Name)value).SelectMany(name => name.Words));
        Assert.Equal($"{shared} integer", name);
    }

    [Fact(DisplayName = $"{import} as name")]
    public void ImportAsName()
    {

        const string declaration = $"{var} {import} {returns} {shared} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Shared);
        Assert.Equal(import, datum.Name.Words[0]);
    }

    [Fact(DisplayName = "name has keywords")]
    public void NameHasKeywords()
    {
        const string declaration = $"{var} {shared} {reactive} {returns} money{end}";

        var datum = Compile(declaration);

        Assert.False(datum.Mutability is Constant);
        Assert.Equal($"{shared} {reactive}", string.Join(' ', datum.Name.Words));
    }

    [Fact(DisplayName = "datatype has keywords")]
    public void DatatypeHasKeywords()
    {
        const string declaration = $"{var} x {returns} {import} {shared} things{end}";

        var datum = Compile(declaration);

        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.NotNull(name);
        Assert.Equal(3, name.Words.Count);
        Assert.Equal(import, name.Words[0]);
        Assert.Equal(shared, name.Words[1]);
        Assert.Equal("things", name.Words[2]);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        const string declaration = $"{var} x {equals} things{end}";

        var datum = Compile(declaration);

        Reference reference = datum.Initializer;
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("things", name.Words[0]);
    }

    [Fact(DisplayName = "explicit initializer is keywords")]
    public void ExplicitInitializerIsKeyword()
    {
        const string declaration = $"{var} x {returns} integer {equals} {import}{end}";

        var datum = Compile(declaration);

        Reference reference = datum.Initializer;
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal(import, name.Words[0]);
    }

    [Fact(DisplayName = "implicit initializer is keywords")]
    public void ImplicitInitializerIsKeyword()
    {
        const string declaration = $"{var} x {equals} {import}{end}";

        var datum = Compile(declaration);

        Reference reference = datum.Initializer;
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal(import, name.Words[0]);
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        const string declaration = $"{var} thing {returns} integer {equals} 2{end}";
        
        var datum = Compile(declaration);

        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("integer", name.Words[0]);

        Temporary value = datum.Initializer;
        Assert.NotNull(value);
        Ronin.Grammar.Scalar scalar = value;
        Assert.NotNull(scalar);
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("2", scalar.Literals[0].Sourcecode.ToString());
    }

    [Fact(DisplayName = "keyword for datatype")]
    public void KeywordForDatatype()
    {
        const string declaration = $"{var} x {returns} {reactive}{end}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Ronin.Grammar.Datum datum = syntax[0];
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.NotNull(name);
        Assert.Equal(reactive, string.Join(' ', name.Words));
    }

    [Fact(DisplayName = "keyword for initializer")]
    public void KeywordForInitializer()
    {
        const string declaration = $"{var} x {returns} integer {equals} {constant}{end}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Ronin.Grammar.Datum datum = syntax[0];
        Assert.NotNull(datum);
        Reference reference = datum.Initializer;
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.NotNull(name);
        Assert.Equal(constant, string.Join(' ', name.Words));
    }


    /*[Fact(DisplayName = "lambda")]
    public void Lambda()
    {

    }*/

    private static Ronin.Grammar.Datum Compile(string declaration)
    {
        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        return syntax[0] as Statement;
    }
}
