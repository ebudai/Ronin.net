using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Datatype = Ronin.Grammar.Datatype;

namespace Unit;

[Trait("Parser", null)]
public class Data : ParsingTests
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        // var my variable => number;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("my"),
            Word("variable"),
            Returns(),
            Word("number"),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Datum.Declaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Shared>());
        Assert.False(datum.Modifiers.Is<Optional>());
        Assert.False(datum.Modifiers.Is<Persistent>());
        Assert.Equal(2, datum.Name?.Source.Length);
        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source.ToArray());
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Reactive.keyword}")]
    public void ReactiveDatatype()
    {
        // let x => reactive text;

        List<Token> tokens = new()
        {
            Keyword.Let(),
            Word("x"),
            Returns(),
            Keyword.Reactive(),
            Word("text"),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Datum.Declaration.Parse(ref parser);

        Assert.IsType<Let>(datum?.Mutability);

        Assert.True(datum.Modifiers.Is<Reactive>());
        Assert.Single(datum.Modifiers.Source.ToArray());

        Assert.Equal(1, datum.Name?.Source.Length);
        
        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source.ToArray());
        
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Compiled.keyword}")]
    public void CompiledDatatype()
    {
        // var x => compiled text;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Returns(),
            Keyword.Compiled(),
            Word("text"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = Datum.Declaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Equal(1, datum.Modifiers?.Source.Length);
        Assert.True(datum.Modifiers.Is<Compiled>());
        
        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source.ToArray());

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Persistent.keyword}")]
    public void PersistentDatatype()
    {
        // constant x => persistent text;

        List<Token> tokens = new()
        {
            Keyword.Constant(),
            Word("x"),
            Returns(),
            Keyword.Persistent(),
            Word("text"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = Datum.Declaration.Parse(ref parser);

        Assert.IsType<Constant>(datum?.Mutability);

        Assert.Equal(1, datum.Modifiers?.Source.Length);
        Assert.True(datum.Modifiers.Is<Persistent>());

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source.ToArray());

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Shared.keyword}")]
    public void SharedDatatype()
    {
        // var x => shared text;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Returns(),
            Keyword.Shared(),
            Word("text"),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Datum.Declaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Equal(1, datum.Modifiers?.Source.Length);
        Assert.True(datum.Modifiers.Is<Shared>());

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source.ToArray());

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Optional.keyword}")]
    public void OptionalDatatype()
    {
        // let x => optional text;

        List<Token> tokens = new()
        {
            Keyword.Let(),
            Word("x"),
            Returns(),
            Keyword.Optional(),
            Word("text"),
            Terminal()
        };

        Parser parser = new(tokens);
        var datum = Datum.Declaration.Parse(ref parser);

        Assert.IsType<Let>(datum?.Mutability);

        Assert.Equal(1, datum.Modifiers?.Source.Length);
        Assert.True(datum.Modifiers.Is<Optional>());

        Assert.Equal(1, datum.Name?.Source.Length);
        
        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source.ToArray());
        
        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "initialized")]
    public void Initialized()
    {
        // var x = things;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Assign(),
            Word("things"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datum = Datum.Declaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Shared>());
        Assert.False(datum.Modifiers.Is<Optional>());
        Assert.False(datum.Modifiers.Is<Persistent>());

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Null(datum.Datatype);

        var unresolved = datum?.Initializer as Value.Unresolved;
        Assert.Single(unresolved?.Reference.Components);
        Name name = unresolved.Reference.Components[0];
        Assert.Single(name?.Source.ToArray());
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        // var thing => number = 2;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("thing"),
            Returns(),
            Word("number"),
            Assign(),
            Number(2),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datum = Datum.Declaration.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Shared>());
        Assert.False(datum.Modifiers.Is<Optional>());
        Assert.False(datum.Modifiers.Is<Persistent>());

        Assert.Equal(1, datum.Name?.Source.Length);

        Assert.Single(datum.Datatype?.Components);
        Name name = datum.Datatype.Components[0];
        Assert.Single(name?.Source.ToArray());

        var scalar = datum.Initializer as Ronin.Grammar.Inline;
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Trait("Analyzer", "declaration")]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string home = nameof(home);
            const string Building = nameof(Building);
            const string test = nameof(test);

            // var home => shared Building = (2, "test", $7);

            Definition module = new()
            {
                Values = new List<Statement>
                {
                    new Datum.Declaration
                    {
                        Mutability = new Variable(),
                        Name = new() { Source = new[] { Word(home) } },
                        Modifiers = new() { Source = new[] { new Shared() } },
                        Datatype = new Reference
                        {
                            Components = new List<Reference.Component>
                            {
                                new() { value = Name(Building) }
                            }
                        },
                        Initializer = new Inputs
                        {
                            Values = new List<Inputs.Input>
                            {
                                new() { value = new Inline { Source = new[] { Number(2) } } },
                                new() { value = new Inline { Source = new[] { Text(test) } } },
                                new() { value = new Inline { Source = new[] { Currency(7) } } }
                            }
                        }
                    }
                }
            };

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Empty(errors);

            Assert.Single(module.Members);

            var entry = module.Members.First();
            var identifier = entry.Key;
            var datum = entry.Value as Datum;

            Assert.IsType<Variable>(datum.Mutability);

            Assert.Single(identifier.value.Source.ToArray());
            Assert.Equal(home, identifier.value.Source.Span[0].Memory.ToArray());

            Assert.IsType<Datatype.Unresolved>(datum.Datatype);

            //Assert.Equal(datum.Initializer.)
        }
    }
}
