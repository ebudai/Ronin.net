using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class datum
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>("my")
            .Add<Word>("variable")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum?.Is.Compiled);
        Assert.False(datum?.Is.Optional);
        Assert.False(datum?.Is.Persistent);
        Assert.False(datum?.Is.Shared);

        Assert.Equal("my variable", string.Join(" ", datum?.Name?.Words ?? new List<string>()));
        
        Name name = datum?.Datatype?.Components?[0];
        Assert.Equal("number", name?.Words?[0]);

        Assert.Null(datum?.Initializer);
    }

    [Fact(DisplayName = $"reactive")]
    public void ReactiveDatatype()
    {
        Tokens tokens = new();
        tokens.Add<Reactive>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Word>("text")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Reactive>(datum.Mutability);

        Assert.False(datum?.Is.Compiled);
        Assert.False(datum?.Is.Optional);
        Assert.False(datum?.Is.Persistent);
        Assert.False(datum?.Is.Shared);

        Assert.Equal("x", datum?.Name?.Words?[0]);

        Name name = datum?.Datatype?.Components?[0];
        Assert.Equal("text", name?.Words?[0]);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"compiled")]
    public void CompiledDatatype()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Compiled>()
            .Add<Word>("text")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum.Mutability);

        Assert.True(datum?.Is.Compiled);
        Assert.False(datum?.Is.Optional);
        Assert.False(datum?.Is.Persistent);
        Assert.False(datum?.Is.Shared);

        Assert.Equal("x", datum?.Name?.Words?[0]);

        Name name = datum?.Datatype?.Components?[0];
        Assert.Equal("text", name?.Words?[0]);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"persistent")]
    public void PersistentDatatype()
    {
        Tokens tokens = new();
        tokens.Add<Constant>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Persistent>()
            .Add<Word>("text")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Constant>(datum.Mutability);

        Assert.False(datum?.Is.Compiled);
        Assert.False(datum?.Is.Optional);
        Assert.True(datum?.Is.Persistent);
        Assert.False(datum?.Is.Shared);

        Assert.Equal("x", datum?.Name?.Words?[0]);

        Name name = datum?.Datatype?.Components?[0];
        Assert.Equal("text", name?.Words?[0]);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"shared")]
    public void SharedDatatype()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Shared>()
            .Add<Word>("text")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum.Mutability);

        Assert.False(datum?.Is.Compiled);
        Assert.False(datum?.Is.Optional);
        Assert.False(datum?.Is.Persistent);
        Assert.True(datum?.Is.Shared);

        Assert.Equal("x", datum?.Name?.Words?[0]);

        Name name = datum?.Datatype?.Components?[0];
        Assert.Equal("text", name?.Words?[0]);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"optional")]
    public void OptionalDatatype()
    {
        Tokens tokens = new();
        tokens.Add<Reactive>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Optional>()
            .Add<Word>("text")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Reactive>(datum.Mutability);

        Assert.False(datum?.Is.Compiled);
        Assert.True(datum?.Is.Optional);
        Assert.False(datum?.Is.Persistent);
        Assert.False(datum?.Is.Shared);

        Assert.Equal("x", datum?.Name?.Words?[0]);

        Name name = datum?.Datatype?.Components?[0];
        Assert.Equal("text", name?.Words?[0]);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>("x")
            .Add<Assign>()
            .Add<Word>("things")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum.Mutability);

        Assert.False(datum?.Is.Compiled);
        Assert.False(datum?.Is.Optional);
        Assert.False(datum?.Is.Persistent);
        Assert.False(datum?.Is.Shared);

        Assert.Equal("x", datum?.Name?.Words?[0]);

        Assert.Null(datum.Datatype);

        Reference reference = datum?.Initializer;
        Name name = reference?.Components?[0];
        Assert.Equal("things", name?.Words?[0]);
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>("thing")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<Assign>()
            .Add<Number>("2")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum.Mutability);

        Assert.False(datum?.Is.Compiled);
        Assert.False(datum?.Is.Optional);
        Assert.False(datum?.Is.Persistent);
        Assert.False(datum?.Is.Shared);

        Assert.Equal("thing", datum?.Name?.Words?[0]);

        Name name = datum?.Datatype?.Components?[0];
        Assert.Equal("number", name?.Words?[0]);

        Scalar scalar = datum?.Initializer;
        Assert.Equal("2", scalar?.Literals?[0]?.ToString());
    }
}
