using Ronin.Compiler;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Reserved;
using Ronin.Lexicon.Symbols;

namespace Unit;

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
    private const string import = Ronin.Lexicon.Reserved.Import.keyword;
    private const string assign = Assign.symbol;

    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        const string declaration = $"{var} my variable {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Variable);
        Assert.False(datum.Is.Constant);
        Assert.Equal("my variable", datum.Name);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Elements);
        Assert.NotEmpty(datum.Datatype.Elements[0].Name.Words);
        var datatype = datum.Datatype.Elements[0].Name.Words[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{reactive}")]
    public void ReactiveDatatype()
    {
        const string declaration = $"{reactive} x {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Reactive);
    }

    [Fact(DisplayName = $"{compiled}")]
    public void CompiledDatatype()
    {
        const string declaration = $"{var} x {returns} {compiled} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Compiled);
    }

    [Fact(DisplayName = $"{persistent}")]
    public void PersistentDatatype()
    {
        const string declaration = $"{constant} x {returns} {persistent} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Constant);
        Assert.True(datum.Modifiers.Persistent);
    }

    [Fact(DisplayName = $"{shared}")]
    public void SharedDatatype()
    {
        const string declaration = $"{var} x {returns} {shared} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Shared);
    }

    [Fact(DisplayName = $"{optional}")]
    public void OptionalDatatype()
    {
        const string declaration = $"{reactive} x {returns} {optional} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Optional);
    }

    [Fact(DisplayName = $"{reactive} twice")]
    public void ReactiveTwiceIsOk()
    {
        const string declaration = $"{reactive} {reactive} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Reactive);
        Assert.Equal($"{reactive} thing", datum.Name);        
    }

    [Fact(DisplayName = $"{constant} twice")]
    public void ConstantTwiceIsOk()
    {
        const string declaration = $"{constant} {constant} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Is.Constant);
        Assert.Equal($"{constant} thing", datum.Name);
    }

    [Fact(DisplayName = $"{var} twice")]
    public void VarTwiceIsOk()
    {
        const string declaration = $"{var} {var} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.False(datum.Is.Constant);
        Assert.Equal($"{var} thing", datum.Name);
    }

    [Fact(DisplayName = $"{compiled} twice")]
    public void CompiledTwiceIsOk()
    {
        const string declaration = $"{compiled} {compiled} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Compiled);
        Assert.Equal($"{compiled} thing", datum.Name);
    }

    [Fact(DisplayName = $"{persistent} twice")]
    public void PersistentTwiceIsOk()
    {
        const string declaration = $"{persistent} {persistent} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Persistent);
        Assert.Equal($"{persistent} thing", datum.Name);
    }

    [Fact(DisplayName = $"{optional} twice")]
    public void OptionalTwiceIsOk()
    {
        const string declaration = $"{optional} {optional} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Optional);
        Assert.Equal($"{optional} thing", datum.Name);
    }

    [Fact(DisplayName = $"{shared} twice")]
    public void SharedTwiceIsOk()
    {
        const string declaration = $"{shared} {shared} thing {returns} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Shared);
        Assert.Equal($"{shared} thing", datum.Name);
    }

    [Fact(DisplayName = $"{import} as name")]
    public void ImportAsName()
    {

        const string declaration = $"{var} {import} {returns} {shared} integer{end}";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Shared);
        Assert.Equal(import, datum.Name);
    }

    [Fact(DisplayName = "name has keywords")]
    public void NameHasKeywords()
    {
        const string declaration = $"{var} {shared} {reactive} {returns} money{end}";

        var datum = Compile(declaration);

        Assert.False(datum.Is.Constant);
        Assert.Equal($"{shared} {reactive}", datum.Name);
    }

    [Fact(DisplayName = "datatype has keywords")]
    public void DatatypeHasKeywords()
    {
        const string declaration = $"{var} x {returns} {import} {shared} things{end}";

        var datum = Compile(declaration);

        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Elements);
        var name = datum.Datatype.Elements[0].Name;
        Assert.NotNull(name);
        Assert.Equal(3, name.Words.Length);
        Assert.Equal(import, name.Words[0]);
        Assert.Equal(shared, name.Words[1]);
        Assert.Equal("things", name.Words[2]);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        const string declaration = $"{var} x {assign} things{end}";

        var datum = Compile(declaration);

        Assert.NotNull(datum.Initializer);
        var name = datum.Initializer.Name;
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal("things", name.Words[0]);
    }

    [Fact(DisplayName = "explicit initializer is keywords")]
    public void ExplicitInitializerIsKeyword()
    {
        const string declaration = $"{var} x {returns} integer {assign} {import}{end}";

        var datum = Compile(declaration);

        Assert.NotNull(datum.Initializer);
        var name = datum.Initializer.Name;
        Assert.NotNull(name);
        Assert.NotEmpty(name.Words);
        Assert.Equal(import, name.Words[0]);
    }

    [Fact(DisplayName = "implicit initializer is keywords")]
    public void ImplicitInitializerIsKeyword()
    {
        const string declaration = $"{var} x {assign} {import}{end}";

        var datum = Compile(declaration);

        Assert.NotNull(datum.Initializer);
        var name = datum.Initializer.Name;
        Assert.NotNull(name);        
        Assert.NotEmpty(name.Words);
        Assert.Equal(import, name.Words[0]);
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        const string declaration = $"{var} thing {returns} integer {assign} 2{end}";
        
        var datum = Compile(declaration);

        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Elements);
        var datatype = datum.Datatype.Elements[0].Name;
        Assert.NotNull(datatype);        
        Assert.NotEmpty(datatype.Words);
        Assert.Equal("integer", datatype.Words[0]);

        Assert.NotNull(datum.Initializer);
        Assert.NotNull(datum.Initializer.Name);
        var initialvalue = datum.Initializer.Scalar;
        Assert.NotEmpty(initialvalue.Elements);
        Assert.Equal("2", initialvalue.Elements[0].Sourcecode.ToString());
    }

    [Fact(DisplayName = "keyword for datatype")]
    public void LiteralForDatatype()
    {
        const string declaration = "var x => reactive;";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
    }

    [Fact(DisplayName = "keyword for initializer")]
    public void LiteralForInitializer()
    {
        const string declaration = "var x => integer = constant;";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
    }


    /*[Fact(DisplayName = "lambda")]
    public void Lambda()
    {

    }*/

    private static Ronin.Grammar.Declaration.Datum Compile(string declaration)
    {
        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        return syntax[0] as Ronin.Grammar.Declaration.Datum;
    }
}
