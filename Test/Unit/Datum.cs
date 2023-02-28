using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Datum
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        // var my variable => number;

        Token[] tokens =
        {
            new VariableKeyword(),
            new Word(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<VariableKeyword>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Equal(2, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{ReactiveKeyword.keyword}")]
    public void ReactiveDatatype()
    {
        // reactive x => text;

        Token[] tokens =
        {
            new ReactiveKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<ReactiveKeyword>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Equal(1, datum.Name?.Source.Length);
        
        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);
        
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{CompiledKeyword.keyword}")]
    public void CompiledDatatype()
    {
        // var x => compiled text;

        Token[] tokens =
        {
            new VariableKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new CompiledKeyword(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<VariableKeyword>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<CompiledKeyword>(datum.Is.Source.Span[0]);
        
        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{PersistentKeyword.keyword}")]
    public void PersistentDatatype()
    {
        // constant x => persistent text;

        Token[] tokens =
        {
            new ConstantKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new PersistentKeyword(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<ConstantKeyword>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<PersistentKeyword>(datum.Is.Source.Span[0]);

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{SharedKeyword.keyword}")]
    public void SharedDatatype()
    {
        // var x => shared text;

        Token[] tokens = 
        {
            new VariableKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new SharedKeyword(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<VariableKeyword>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<SharedKeyword>(datum.Is.Source.Span[0]);

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{OptionalKeyword.keyword}")]
    public void OptionalDatatype()
    {
        // reactive x => optional text;

        Token[] tokens =
        {
            new ReactiveKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new OptionalKeyword(),
            new Word(),
            new TerminalSymbol()
        };

        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<ReactiveKeyword>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<OptionalKeyword>(datum.Is.Source.Span[0]);

        Assert.Equal(1, datum.Name?.Source.Length);
        
        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);
        
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        // var x = things;

        Token[] tokens =
        {
            new VariableKeyword(),
            new Word(),
            new AssignSymbol(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<VariableKeyword>(datum?.Mutability);

        Assert.Null(datum.Is);
        
        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Null(datum.Datatype);

        Ronin.Grammar.Reference reference = datum?.Initializer;
        Assert.Single(reference?.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        // var thing => number = 2;

        Token[] tokens =
        {
            new VariableKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new AssignSymbol(),
            new NumberLiteral(),
            new TerminalSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);

        Assert.IsType<VariableKeyword>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        LiteralSyntax scalar = datum.Initializer;
        Assert.Equal(1, scalar?.Source.Length);
    }
}
