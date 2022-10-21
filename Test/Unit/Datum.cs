using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Token;
using Ronin.Token.Symbols;

namespace Unit;

public class Datum
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        const string declaration = "var my variable => integer;";

        var datum = Compile(declaration);

        Assert.False(datum.IsReadonly);
        Assert.Equal("my variable", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "reactive")]
    public void Reactive()
    {
        const string declaration = "reactive x => integer;";

        var datum = Compile(declaration);

        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsOptional);
        Assert.False(datum.IsPersistent);
        Assert.True(datum.IsReactive);
        Assert.False(datum.IsReadonly);        
        Assert.False(datum.IsShared);        
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "compiled")]
    public void Compiled()
    {
        const string declaration = "compiled x => integer;";

        var datum = Compile(declaration);

        Assert.True(datum.IsCompiled);
        Assert.False(datum.IsOptional);
        Assert.False(datum.IsPersistent);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsReadonly);
        Assert.False(datum.IsShared);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "persistent")]
    public void Persistent()
    {
        const string declaration = "persistent x => integer;";

        var datum = Compile(declaration);

        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsOptional);
        Assert.True(datum.IsPersistent);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsReadonly);
        Assert.False(datum.IsShared);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "shared")]
    public void Shared()
    {
        const string declaration = "shared x => integer;";

        var datum = Compile(declaration);

        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsOptional);
        Assert.False(datum.IsPersistent);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsReadonly);
        Assert.True(datum.IsShared);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "optional")]
    public void Optional()
    {
        const string declaration = "optional x => integer;";

        var datum = Compile(declaration);

        Assert.False(datum.IsCompiled);
        Assert.True(datum.IsOptional);
        Assert.False(datum.IsPersistent);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsReadonly);
        Assert.False(datum.IsShared);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "reactive twice")]
    public void ReactiveTwiceIsOk()
    {
        const string declaration = "reactive reactive thing => integer;";

        var datum = Compile(declaration);

        Assert.True(datum.IsReactive);
        Assert.Equal($"{Ronin.Token.Keywords.Reactive.keyword} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "constant twice")]
    public void ConstantTwiceIsOk()
    {
        const string declaration = "constant constant thing => integer;";

        var datum = Compile(declaration);

        Assert.True(datum.IsReadonly);
        Assert.Equal($"{Ronin.Token.Keywords.Constant.keyword} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "var twice")]
    public void VarTwiceIsOk()
    {
        const string var = Ronin.Token.Keywords.Variable.keyword;
        const string declaration = $"{var} {var} thing {Returns.character} integer;";

        var datum = Compile(declaration);

        Assert.False(datum.IsReadonly);
        Assert.Equal($"{var} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "compiled twice")]
    public void CompiledTwiceIsOk()
    {
        const string declaration = "compiled compiled thing => integer;";

        var datum = Compile(declaration);

        Assert.True(datum.IsCompiled);
        Assert.Equal($"{Ronin.Token.Keywords.Compiled.keyword} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "persistent twice")]
    public void PersistentTwiceIsOk()
    {
        const string declaration = "persistent persistent thing => integer;";

        var datum = Compile(declaration);

        Assert.True(datum.IsPersistent);
        Assert.Equal($"{Ronin.Token.Keywords.Persistent.keyword} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "optional twice")]
    public void OptionalTwiceIsOk()
    {
        const string declaration = "optional optional thing => integer;";

        var datum = Compile(declaration);

        Assert.True(datum.IsOptional);
        Assert.Equal($"{Ronin.Token.Keywords.Optional.keyword} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "shared twice")]
    public void SharedTwiceIsOk()
    {
        const string declaration = "shared shared thing => integer;";

        var datum = Compile(declaration);

        Assert.True(datum.IsShared);
        Assert.Equal($"{Ronin.Token.Keywords.Shared.keyword} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "import as name")]
    public void ImportAsName()
    {
        const string declaration = "shared import => integer;";

        var datum = Compile(declaration);

        Assert.True(datum.IsShared);
        Assert.Equal("import", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "name has keywords")]
    public void NameHasKeywords()
    {
        const string declaration = "var shared reactive => money;";

        var datum = Compile(declaration);

        Assert.False(datum.IsReadonly);
        Assert.Equal($"{Ronin.Token.Keywords.Shared.keyword} {Ronin.Token.Keywords.Reactive.keyword}", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        string datatype = datum.Datatype.Names[0].Names[0];
        Assert.Equal("money", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "datatype has keywords")]
    public void DatatypeHasKeywords()
    {
        const string declaration = $"var x => {Ronin.Token.Keywords.Import.keyword} {Ronin.Token.Keywords.Shared.keyword} things;";

        var datum = Compile(declaration);

        Assert.False(datum.IsReadonly);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.Equal(3, datum.Datatype.Names[0].Names.Length);
        var datatype = datum.Datatype.Names[0].Names;
        Assert.Equal($"{Ronin.Token.Keywords.Import.keyword} {Ronin.Token.Keywords.Shared.keyword} things", string.Join(' ', datatype));
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        const string declaration = "var x = things;";

        var datum = Compile(declaration);

        Assert.False(datum.IsReadonly);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Initializer);
        Assert.NotEmpty(datum.Initializer.Names);
        Assert.NotEmpty(datum.Initializer.Names[0].Names);
        Assert.Equal("things", datum.Initializer.Names[0].Names[0]);
        Assert.Null(datum.Datatype); 
    }

    [Fact(DisplayName = "explicit initializer is keywords")]
    public void ExplicitInitializerIsKeyword()
    {
        const string declaration = "var x => integer = import;";

        var datum = Compile(declaration);

        Assert.False(datum.IsReadonly);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Initializer);
        Assert.NotEmpty(datum.Initializer.Names);
        Assert.NotEmpty(datum.Initializer.Names[0].Names);
        Assert.Equal($"{Ronin.Token.Keywords.Import.keyword}", datum.Initializer.Names[0].Names[0]);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names);
        Assert.Equal("integer", datum.Datatype.Names[0].Names[0]);
    }

    [Fact(DisplayName = "implicit initializer is keywords")]
    public void ImplicitInitializerIsKeyword()
    {
        const string declaration = "var x = import;";

        var datum = Compile(declaration);

        Assert.False(datum.IsReadonly);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Initializer);
        Assert.NotEmpty(datum.Initializer.Names);
        Assert.NotEmpty(datum.Initializer.Names[0].Names);
        Assert.Equal($"{Ronin.Token.Keywords.Import.keyword}", datum.Initializer.Names[0].Names[0]);
        Assert.Null(datum.Datatype);
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        const string declaration = "var thing => integer = 2;";
        
        var datum = Compile(declaration);
        
        Assert.False(datum.IsReadonly);
        Assert.Equal("thing", datum.Identifier);
        Assert.NotNull(datum.Initializer);
        Assert.NotEmpty(datum.Initializer.Arguments);
        Assert.NotNull(datum.Initializer.Arguments[0].Scalar);
        var scalar = datum.Initializer.Arguments[0].Scalar;
        Assert.NotEmpty(scalar.Literals);
        Assert.Equal("2", scalar.Literals[0].ToString());
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Names);
        Assert.NotEmpty(datum.Datatype.Names[0].Names[0]);
        Assert.Equal("integer", datum.Datatype.Names[0].Names[0]);
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
