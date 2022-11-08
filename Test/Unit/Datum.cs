using Ronin.Compiler;
using Ronin.Lexicon.Reserved;
using Ronin.Lexicon.Symbols;
using static Ronin.Grammar.Declaration.Datum;

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
        const string declaration = $"{var} my variable {returns} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Declarator.Variable);
        Assert.Equal("my variable", datum.Name);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Values);
        Assert.NotEmpty(datum.Datatype.Values[0].Name.Words);
        var datatype = datum.Datatype.Values[0].Name.Words[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{reactive}")]
    public void ReactiveDatatype()
    {
        const string declaration = $"{reactive} x {returns} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Declarator.Reactive);
    }

    [Fact(DisplayName = $"{compiled}")]
    public void CompiledDatatype()
    {
        const string declaration = $"{var} x {returns} {compiled} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Compiled);
    }

    [Fact(DisplayName = $"{persistent}")]
    public void PersistentDatatype()
    {
        const string declaration = $"{constant} x {returns} {persistent} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Declarator.Constant);
        Assert.True(datum.Modifiers.Persistent);
    }

    [Fact(DisplayName = $"{shared}")]
    public void SharedDatatype()
    {
        const string declaration = $"{var} x {returns} {shared} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Shared);
    }

    [Fact(DisplayName = $"{optional}")]
    public void OptionalDatatype()
    {
        const string declaration = $"{reactive} x {returns} {optional} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Optional);
    }

    [Fact(DisplayName = $"{reactive} twice")]
    public void ReactiveTwiceIsOk()
    {
        const string declaration = $"{reactive} {reactive} thing {returns} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Declarator.Reactive);
        Assert.Equal($"{reactive} thing", datum.Name);        
    }

    [Fact(DisplayName = $"{constant} twice")]
    public void ConstantTwiceIsOk()
    {
        const string declaration = $"{constant} {constant} thing {returns} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Mutability is Declarator.Constant);
        Assert.Equal($"{constant} thing", datum.Name);
    }

    [Fact(DisplayName = $"{var} twice")]
    public void VarTwiceIsOk()
    {
        const string declaration = $"{var} {var} thing {returns} integer";

        var datum = Compile(declaration);

        Assert.False(datum.Mutability is Declarator.Constant);
        Assert.Equal($"{var} thing", datum.Name);
    }

    [Fact(DisplayName = $"{compiled} twice")]
    public void CompiledTwiceIsOk()
    {
        const string declaration = $"{var} thing {returns} {compiled} {compiled} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Compiled);
        var name = string.Join(' ', datum.Datatype.Values.Select(value => value.Name).SelectMany(name => name.Words));
        Assert.Equal($"{compiled} integer", name);
    }

    [Fact(DisplayName = $"{persistent} twice")]
    public void PersistentTwiceIsOk()
    {
        const string declaration = $"{var} thing {returns} {persistent} {persistent} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Persistent);
        var name = string.Join(' ', datum.Datatype.Values.Select(value => value.Name).SelectMany(name => name.Words));
        Assert.Equal($"{persistent} integer", name);
    }

    [Fact(DisplayName = $"{optional} twice")]
    public void OptionalTwiceIsOk()
    {
        const string declaration = $"{var} thing {returns} {optional} {optional} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Optional);
        var name = string.Join(' ', datum.Datatype.Values.Select(value => value.Name).SelectMany(name => name.Words));
        Assert.Equal($"{optional} integer", name);
    }

    [Fact(DisplayName = $"{shared} twice")]
    public void SharedTwiceIsOk()
    {
        const string declaration = $"{var} thing {returns} {shared} {shared} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Shared);
        var name = string.Join(' ', datum.Datatype.Values.Select(value => value.Name).SelectMany(name => name.Words));
        Assert.Equal($"{shared} integer", name);
    }

    [Fact(DisplayName = $"{import} as name")]
    public void ImportAsName()
    {

        const string declaration = $"{var} {import} {returns} {shared} integer";

        var datum = Compile(declaration);

        Assert.True(datum.Modifiers.Shared);
        Assert.Equal(import, datum.Name);
    }

    [Fact(DisplayName = "name has keywords")]
    public void NameHasKeywords()
    {
        const string declaration = $"{var} {shared} {reactive} {returns} money";

        var datum = Compile(declaration);

        Assert.False(datum.Mutability is Declarator.Constant);
        Assert.Equal($"{shared} {reactive}", datum.Name);
    }

    [Fact(DisplayName = "datatype has keywords")]
    public void DatatypeHasKeywords()
    {
        const string declaration = $"{var} x {returns} {import} {shared} things";

        var datum = Compile(declaration);

        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Values);
        var name = datum.Datatype.Values[0].Name;
        Assert.NotNull(name);
        Assert.Equal(3, name.Words.Length);
        Assert.Equal(import, name.Words[0]);
        Assert.Equal(shared, name.Words[1]);
        Assert.Equal("things", name.Words[2]);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        const string declaration = $"{var} x {assign} things";

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
        const string declaration = $"{var} x {returns} integer {assign} {import}";

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
        const string declaration = $"{var} x {assign} {import}";

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
        const string declaration = $"{var} thing {returns} integer {assign} 2";
        
        var datum = Compile(declaration);

        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Values);
        var datatype = datum.Datatype.Values[0].Name;
        Assert.NotNull(datatype);
        Assert.NotEmpty(datatype.Words);
        Assert.Equal("integer", datatype.Words[0]);

        Assert.NotNull(datum.Initializer);
        Assert.NotNull(datum.Initializer.Scalar);
        var initialvalue = datum.Initializer.Scalar;
        Assert.NotEmpty(initialvalue.Values);
        Assert.Equal("2", initialvalue.Values[0].Sourcecode.ToString());
    }

    [Fact(DisplayName = "keyword for datatype")]
    public void KeywordForDatatype()
    {
        const string declaration = $"{var} x {returns} {reactive}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(ref tokens[0]);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        Assert.NotNull(statement.Datum?.Datatype);
        var datatype = statement.Datum.Datatype;
        Assert.NotEmpty(datatype.Values);
        Assert.NotNull(datatype.Values[0].Name);
        var name = datatype.Values[0].Name;
        Assert.Equal(reactive, string.Join(' ', name.Words));
    }

    [Fact(DisplayName = "keyword for initializer")]
    public void KeywordForInitializer()
    {
        const string declaration = $"{var} x {returns} integer {assign} {constant}";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(ref tokens[0]);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        Assert.NotNull(statement.Datum?.Initializer);
        var initializer = statement.Datum.Initializer;
        Assert.NotNull(initializer.Name);
        var name = initializer.Name;
        Assert.Equal(constant, string.Join(' ', name.Words));
    }


    /*[Fact(DisplayName = "lambda")]
    public void Lambda()
    {

    }*/

    private static Ronin.Grammar.Declaration.Datum Compile(string declaration)
    {
        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(ref tokens[0]);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        return statement.Datum;
    }
}
