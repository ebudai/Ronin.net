using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class DatumDeclarations : ParsingTests
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        // var my variable => number;

        List<Token> tokens = new()
        {
            Variable(),
            Word("my"),
            Word("variable"),
            Returns(),
            Word("number"),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Null(datum.Is);

        {
            Assert.Single(datum.Name?.Components);
            Ronin.Grammar.Words name = datum.Name.Components[0];
            Assert.Equal(2, name?.Source.Length);
        }

        {
            Assert.Single(datum.Datatype?.Components);
            Ronin.Grammar.Words name = datum.Datatype.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Reactive.keyword}")]
    public void ReactiveDatatype()
    {
        // reactive x => text;

        List<Token> tokens = new()
        {
            Reactive(),
            Word("x"),
            Returns(),
            Word("text"),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Reactive>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Single(datum.Name?.Components);
        
        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Words name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);
        
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Compiled.keyword}")]
    public void CompiledDatatype()
    {
        // var x => compiled text;

        List<Token> tokens = new()
        {
            Variable(),
            Word("x"),
            Returns(),
            Compiled(),
            Word("text"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<Compiled>(parser[datum.Is.Source.Start]);
        
        Assert.Single(datum.Name?.Components);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Words name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Persistent.keyword}")]
    public void PersistentDatatype()
    {
        // constant x => persistent text;

        List<Token> tokens = new()
        {
            Constant(),
            Word("x"),
            Returns(),
            Persistent(),
            Word("text"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Constant>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<Persistent>(parser[datum.Is.Source.Start]);

        Assert.Single(datum.Name?.Components);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Words name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Shared.keyword}")]
    public void SharedDatatype()
    {
        // var x => shared text;

        List<Token> tokens = new()
        {
            Variable(),
            Word("x"),
            Returns(),
            Shared(),
            Word("text"),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<Shared>(parser[datum.Is.Source.Start]);

        Assert.Single(datum.Name?.Components);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Words name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Optional.keyword}")]
    public void OptionalDatatype()
    {
        // reactive x => optional text;

        List<Token> tokens = new()
        {
            Reactive(),
            Word("x"),
            Returns(),
            Optional(),
            Word("text"),
            Terminal()
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Reactive>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<Optional>(parser[datum.Is.Source.Start]);

        Assert.Single(datum.Name?.Components);
        
        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Words name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);
        
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        // var x = things;

        List<Token> tokens = new()
        {
            Variable(),
            Word("x"),
            Assign(),
            Word("things"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Null(datum.Is);
        
        Assert.Single(datum.Name?.Components);

        Assert.Null(datum.Datatype);

        var reference = datum?.Initializer as Ronin.Grammar.Reference;
        Assert.Single(reference?.Components);
        Ronin.Grammar.Words name = reference.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        // var thing => number = 2;

        List<Token> tokens = new()
        {
            Variable(),
            Word("thing"),
            Returns(),
            Word("number"),
            Assign(),
            Number(2),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Single(datum.Name?.Components);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Words name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        var scalar = datum.Initializer as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }
}
