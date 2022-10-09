using static Ronin.Token.Keyword.Word;

namespace Unit;

public class Datum
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        const string declaration = "var my variable => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsReadonly);
        Assert.Equal("my variable", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "reactive")]
    public void Reactive()
    {
        const string declaration = "reactive x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsOptional);
        Assert.False(datum.IsPersistent);
        Assert.True(datum.IsReactive);
        Assert.False(datum.IsReadonly);        
        Assert.False(datum.IsShared);        
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "compiled")]
    public void Compiled()
    {
        const string declaration = "compiled x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.True(datum.IsCompiled);
        Assert.False(datum.IsOptional);
        Assert.False(datum.IsPersistent);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsReadonly);
        Assert.False(datum.IsShared);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "persistent")]
    public void Persistent()
    {
        const string declaration = "persistent x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsOptional);
        Assert.True(datum.IsPersistent);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsReadonly);
        Assert.False(datum.IsShared);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "shared")]
    public void Shared()
    {
        const string declaration = "shared x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsOptional);
        Assert.False(datum.IsPersistent);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsReadonly);
        Assert.True(datum.IsShared);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "optional")]
    public void Optional()
    {
        const string declaration = "optional x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsCompiled);
        Assert.True(datum.IsOptional);
        Assert.False(datum.IsPersistent);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsReadonly);
        Assert.False(datum.IsShared);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "reactive twice")]
    public void ReactiveTwiceIsOk()
    {
        const string declaration = "reactive reactive thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.True(datum.IsReactive);
        Assert.Equal($"{nameof(reactive)} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "constant twice")]
    public void ConstantTwiceIsOk()
    {
        const string declaration = "constant constant thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.True(datum.IsReadonly);
        Assert.Equal($"{nameof(constant)} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "var twice")]
    public void VarTwiceIsOk()
    {
        const string declaration = "var var thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsReadonly);
        Assert.Equal($"{nameof(var)} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "compiled twice")]
    public void CompiledTwiceIsOk()
    {
        const string declaration = "compiled compiled thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.True(datum.IsCompiled);
        Assert.Equal($"{nameof(compiled)} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "persistent twice")]
    public void PersistentTwiceIsOk()
    {
        const string declaration = "persistent persistent thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.True(datum.IsPersistent);
        Assert.Equal($"{nameof(persistent)} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "optional twice")]
    public void OptionalTwiceIsOk()
    {
        const string declaration = "optional optional thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.True(datum.IsOptional);
        Assert.Equal($"{nameof(optional)} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "shared twice")]
    public void SharedTwiceIsOk()
    {
        const string declaration = "shared shared thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.True(datum.IsShared);
        Assert.Equal($"{nameof(shared)} thing", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "import as name")]
    public void ImportAsName()
    {
        const string declaration = "shared import => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.True(datum.IsShared);
        Assert.Equal("import", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("integer", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "name has keywords")]
    public void NameHasKeywords()
    {
        const string declaration = "var shared reactive => money;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsReadonly);
        Assert.Equal($"{nameof(shared)} {nameof(reactive)}", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        var datatype = datum.Datatype.Name[0].AsT0;
        Assert.Equal("money", datatype);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "datatype has keywords")]
    public void DatatypeHasKeywords()
    {
        const string declaration = "var x => import shared things;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsReadonly);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Datatype);
        Assert.Equal(3, datum.Datatype.Name.Count);
        Assert.True(datum.Datatype.Name[0].IsT0);
        Assert.True(datum.Datatype.Name[1].IsT0);
        Assert.True(datum.Datatype.Name[2].IsT0);
        var datatype = datum.Datatype.Name;
        Assert.Equal($"{nameof(import)} {nameof(shared)} things", datatype[0].AsT0 + ' ' + datatype[1].AsT0 + ' ' + datatype[2].AsT0);
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        const string declaration = "var x = things;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsReadonly);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Initializer);
        Assert.NotEmpty(datum.Initializer.Name);
        Assert.True(datum.Initializer.Name[0].IsT0); // name is a string
        Assert.Equal("things", datum.Initializer.Name[0].AsT0);
        Assert.Null(datum.Datatype); 
    }

    [Fact(DisplayName = "explicit initializer is keywords")]
    public void ExplicitInitializerIsKeyword()
    {
        const string declaration = "var x => integer = import;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        var datum = syntax[0] as Ronin.Grammar.Declaration.Datum;
        Assert.False(datum.IsReadonly);
        Assert.Equal("x", datum.Identifier);
        Assert.NotNull(datum.Initializer);
        Assert.NotEmpty(datum.Initializer.Name);
        Assert.True(datum.Initializer.Name[0].IsT0); // name is a string
        Assert.Equal($"{nameof(import)}", datum.Initializer.Name[0].AsT0);
        Assert.NotNull(datum.Datatype);
        Assert.NotEmpty(datum.Datatype.Name);
        Assert.True(datum.Datatype.Name[0].IsT0); // name is a string
        Assert.Equal("integer", datum.Datatype.Name[0].AsT0);
    }

    /*[Fact(DisplayName = "implicit initializer is keywords")]
    public void ImplicitInitializerIsKeyword()
    {
        const string declaration = "var x = import;";
        var datum = Compile(declaration);
        Assert.Equal("import", datum.Initializer.Name);
    }

    [Fact(DisplayName = "typed and initialized")]
    public void TypedAndInitialized()
    {
        const string declaration = "var thing => integer = 2;";
        var datum = Compile(declaration);
        Assert.Equal("thing", datum.Name);
        Assert.Equal("integer", datum.Datatype.Name);
        //TODO: Assert.Equal("2", datum.Initializer.Value);
    }

    [Fact(DisplayName = "lambda")]
    public void Lambda()
    {

    }

    private static Ronin.Language.Datum Compile(string declaration)
    {
        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Language.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        return datum;
    }*/
}
