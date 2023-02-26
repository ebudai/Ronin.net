using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class datum
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        // var my variable => number;

        Token[] tokens =
        {
            new Variable(),
            new Word(),
            new Word(),
            new Returns(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Equal(2, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Reactive.keyword}")]
    public void ReactiveDatatype()
    {
        // reactive x => text;

        Token[] tokens =
        {
            new Reactive(),
            new Word(),
            new Returns(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<Reactive>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Single(datum.Name?.Source);
        
        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source);
        
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Compiled.keyword}")]
    public void CompiledDatatype()
    {
        // var x => compiled text;

        Token[] tokens =
        {
            new Variable(),
            new Word(),
            new Returns(),
            new Compiled(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Single(datum.Is?.Source);
        Assert.IsType<Compiled>(datum.Is.Source[0]);
        
        Assert.Single(datum.Name?.Source);

        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Persistent.keyword}")]
    public void PersistentDatatype()
    {
        // constant x => persistent text;

        Token[] tokens =
        {
            new Constant(),
            new Word(),
            new Returns(),
            new Persistent(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<Constant>(datum?.Mutability);

        Assert.Single(datum.Is?.Source);
        Assert.IsType<Persistent>(datum.Is.Source[0]);

        Assert.Single(datum.Name?.Source);

        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Shared.keyword}")]
    public void SharedDatatype()
    {
        // var x => shared text;

        Token[] tokens = 
        {
            new Variable(),
            new Word(),
            new Returns(),
            new Shared(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Single(datum.Is?.Source);
        Assert.IsType<Shared>(datum.Is.Source[0]);

        Assert.Single(datum.Name?.Source);

        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Optional.keyword}")]
    public void OptionalDatatype()
    {
        // reactive x => optional text;

        Token[] tokens =
        {
            new Reactive(),
            new Word(),
            new Returns(),
            new Optional(),
            new Word(),
            new Terminal()
        };

        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<Reactive>(datum?.Mutability);

        Assert.Single(datum.Is?.Source);
        Assert.IsType<Optional>(datum.Is.Source[0]);

        Assert.Single(datum.Name?.Source);
        
        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source);
        
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        // var x = things;

        Token[] tokens =
        {
            new Variable(),
            new Word(),
            new Assign(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Null(datum.Is);
        
        Assert.Single(datum.Name?.Source);

        Assert.Null(datum.Datatype);

        Reference reference = datum?.Initializer;
        Assert.Single(reference?.Components);
        Name name = reference.Components[0];
        Assert.Single(name?.Source);
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        // var thing => number = 2;

        Token[] tokens =
        {
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new Assign(),
            new Number(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Single(datum.Name?.Source);

        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source);

        LiteralSyntax scalar = datum.Initializer;
        Assert.Single(scalar?.Source);
    }
}
