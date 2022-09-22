using Ronin.Grammar;
using Ronin.Token;

namespace Unit;

public class Datum
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        const string declaration = "var my variable => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        
        while (tokens.TryDequeue(out var token))
        {
            var result = datum.Add(token);
            Assert.Equal(token is Ronin.Token.Symbol symbol && symbol.IsTerminal ? Syntax.Result.Completed : Syntax.Result.Applied, result);
        }
    }

    [Fact(DisplayName = "reactive")]
    public void Reactive()
    {
        const string declaration = "reactive x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.True(datum.IsReactive);
        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsPersistent);
        Assert.False(datum.IsShared);
        Assert.False(datum.IsOptional);
    }

    [Fact(DisplayName = "compiled")]
    public void Compiled()
    {
        const string declaration = "compiled x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.False(datum.IsReactive);
        Assert.True(datum.IsCompiled);
        Assert.False(datum.IsPersistent);
        Assert.False(datum.IsShared);
        Assert.False(datum.IsOptional);
    }

    [Fact(DisplayName = "persistent")]
    public void Persistent()
    {
        const string declaration = "persistent x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsCompiled);
        Assert.True(datum.IsPersistent);
        Assert.False(datum.IsShared);
        Assert.False(datum.IsOptional);
    }

    [Fact(DisplayName = "shared")]
    public void Shared()
    {
        const string declaration = "shared x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsPersistent);
        Assert.True(datum.IsShared);
        Assert.False(datum.IsOptional);
    }

    [Fact(DisplayName = "optional")]
    public void Optional()
    {
        const string declaration = "optional x => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.False(datum.IsReactive);
        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsPersistent);
        Assert.False(datum.IsShared);
        Assert.True(datum.IsOptional);
    }

    [Fact(DisplayName = "reactive twice")]
    public void ReactiveTwiceIsOk()
    {
        const string declaration = "reactive reactive thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.True(datum.IsReactive);
        Assert.Equal("reactive thing", datum.Name);
    }

    [Fact(DisplayName = "constant twice")]
    public void ConstantTwiceIsOk()
    {
        const string declaration = "constant constant thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.True(datum.IsReadonly);
        Assert.Equal("constant thing", datum.Name);
    }

    [Fact(DisplayName = "var twice")]
    public void VarTwiceIsOk()
    {
        const string declaration = "var var thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.False(datum.IsReadonly);
        Assert.Equal("var thing", datum.Name);
    }

    [Fact(DisplayName = "compiled twice")]
    public void CompiledTwiceIsOk()
    {
        const string declaration = "compiled compiled thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.True(datum.IsCompiled);
        Assert.Equal("compiled thing", datum.Name);
    }

    [Fact(DisplayName = "persistent twice")]
    public void PersistentTwiceIsOk()
    {
        const string declaration = "persistent persistent thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.True(datum.IsPersistent);
        Assert.Equal("persistent thing", datum.Name);
    }

    [Fact(DisplayName = "optional twice")]
    public void OptionalTwiceIsOk()
    {
        const string declaration = "optional optional thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.True(datum.IsOptional);
        Assert.Equal("optional thing", datum.Name);
    }

    [Fact(DisplayName = "shared twice")]
    public void SharedTwiceIsOk()
    {
        const string declaration = "shared shared thing => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.True(datum.IsShared);
        Assert.Equal("shared thing", datum.Name);
    }

    [Fact(DisplayName = "import as name")]
    public void ImportAsName()
    {
        const string declaration = "shared import => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.Equal("import", datum.Name);
    }

    [Fact(DisplayName = "name has keywords")]
    public void NameHasKeywords()
    {
        const string declaration = "var shared reactive => money;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.Equal("shared reactive", datum.Name);
    }

    [Fact(DisplayName = "datatype has keywords")]
    public void DatatypeHasKeywords()
    {
        const string declaration = "var x => import shared things;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.Equal("import shared things", string.Join(' ', datum.Datatype.Name.Name.Values));
    }

    [Fact(DisplayName = "explicit initializer is keywords")]
    public void ExplicitInitializerIsKeyword()
    {
        const string declaration = "var x => integer = import;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.Equal("import", string.Join(' ', datum.Initializer.Name.Name.Values));
    }

    [Fact(DisplayName = "implicit initializer is keywords")]
    public void ImplicitInitializerIsKeyword()
    {
        const string declaration = "var x = import;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Grammar.Datum datum = new();
        while (tokens.TryDequeue(out var token)) datum.Add(token);
        Assert.Equal("import", string.Join(' ', datum.Initializer.Name.Name.Values));
    }
}
