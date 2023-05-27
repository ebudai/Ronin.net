using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class DatumDeclaration
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        // var my variable => number;

        Token[] tokens =
        {
            new Variable { sourcecode = Variable.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Word { sourcecode = "variable".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Word { sourcecode = "number".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Null(datum.Is);

        {
            Assert.Single(datum.Name?.Components);
            Ronin.Grammar.Name name = datum.Name.Components[0];
            Assert.Equal(2, name?.Source.Length);
        }

        {
            Assert.Single(datum.Datatype?.Components);
            Ronin.Grammar.Name name = datum.Datatype.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Reactive.keyword}")]
    public void ReactiveDatatype()
    {
        // reactive x => text;

        Token[] tokens =
        {
            new Reactive { sourcecode = Reactive.keyword.AsMemory() },
            new Word { sourcecode = "x".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Word { sourcecode = "text".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Reactive>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Single(datum.Name?.Components);
        
        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);
        
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Compiled.keyword}")]
    public void CompiledDatatype()
    {
        // var x => compiled text;

        Token[] tokens =
        {
            new Variable { sourcecode = Variable.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Compiled { sourcecode = Compiled.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<Compiled>(datum.Is.Source.Span[0]);
        
        Assert.Single(datum.Name?.Components);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Persistent.keyword}")]
    public void PersistentDatatype()
    {
        // constant x => persistent text;

        Token[] tokens =
        {
            new Constant { sourcecode = Constant.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Persistent { sourcecode = Persistent.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Constant>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<Persistent>(datum.Is.Source.Span[0]);

        Assert.Single(datum.Name?.Components);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Shared.keyword}")]
    public void SharedDatatype()
    {
        // var x => shared text;

        Token[] tokens = 
        {
            new Variable { sourcecode = Variable.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Shared { sourcecode = Shared.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<Shared>(datum.Is.Source.Span[0]);

        Assert.Single(datum.Name?.Components);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Optional.keyword}")]
    public void OptionalDatatype()
    {
        // reactive x => optional text;

        Token[] tokens =
        {
            new Reactive { sourcecode = Reactive.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Optional { sourcecode = Optional.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Terminal()
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Reactive>(datum?.Mutability);

        Assert.Equal(1, datum.Is?.Source.Length);
        Assert.IsType<Optional>(datum.Is.Source.Span[0]);

        Assert.Single(datum.Name?.Components);
        
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
            new Variable { sourcecode = Variable.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Assign { sourcecode = Assign.symbol.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
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
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        // var thing => number = 2;

        Token[] tokens =
        {
            new Variable { sourcecode = Variable.keyword.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Word { sourcecode = "my".AsMemory() },
            new Assign { sourcecode = Assign.symbol.AsMemory() },
            new NumberLiteral { sourcecode = "2".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Single(datum.Name?.Components);

        Assert.Single(datum.Datatype?.Components);
        Ronin.Grammar.Name name = datum.Datatype.Components[0];
        Assert.Equal(1, name?.Source.Length);

        var scalar = datum.Initializer as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }
}
