using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Type = Ronin.Grammar.Type;

namespace Unit;

[Trait(nameof(Parser), null)]
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
            Arrow(),
            Word("number"),
            Terminal(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());
        Assert.Single(datum.Identifier);
        var unresolved = datum.Type as Type.Unresolved;
        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());
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
            Arrow(),
            Keyword.Reactive(),
            Word("text"),
            Terminal(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Let>(datum?.Mutability);

        Assert.True(datum.Modifiers.Is<Reactive>());
        Assert.Single(datum.Modifiers.Tokens.ToArray());

        Assert.Single(datum.Identifier);

        var unresolved = datum.Type as Type.Unresolved;
        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());
        
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
            Arrow(),
            Keyword.Compiled(),
            Word("text"),
            Terminal(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Single(datum.Modifiers?.Tokens.ToArray());
        Assert.True(datum.Modifiers.Is<Compiled>());
        
        Assert.Single(datum.Identifier);

        var unresolved = datum.Type as Type.Unresolved;
        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = $"{Global.keyword}")]
    public void SharedDatatype()
    {
        // var x => shared text;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Arrow(),
            Keyword.Shared(),
            Word("text"),
            Terminal(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Single(datum.Modifiers?.Tokens.ToArray());
        Assert.True(datum.Modifiers.Is<Global>());

        Assert.Single(datum.Identifier);

        var unresolved = datum.Type as Type.Unresolved;
        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        Assert.Null(datum.Initializer);
    }

    [Fact(DisplayName = "an optional type is a type, not a modifier")]
    public void AnOptionalTypeIsATypeNotAModifier()
    {
        // let x => optional text;
        //
        // «optional» was a MODIFIER keyword, so this parsed as a modified
        // declaration of type «text». It is the pattern «optional (_)» now — the
        // last type constructor that was not one — so the type is two words and
        // the declaration has no modifiers at all.
        List<Token> tokens = new()
        {
            Keyword.Let(),
            Word("x"),
            Arrow(),
            Word("optional"),
            Word("text"),
            Terminal()
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Let>(datum?.Mutability);
        Assert.Empty(datum.Modifiers?.Tokens.ToArray());

        var unresolved = datum.Type as Type.Unresolved;

        Assert.Equal(["optional", "text"], unresolved?.Reference.ToLexemes().Select(lexeme => lexeme.Text));
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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());

        Assert.Single(datum.Identifier);

        Assert.Null(datum.Type);

        var member = datum.Initializer as Member.Unresolved;
        Assert.Single(member?.Reference);
        var name = member.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());
    }

    [Fact(DisplayName = "typed and initialized via literal")]
    public void TypedAndInitialized()
    {
        // var thing => number = 2;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("thing"),
            Arrow(),
            Word("number"),
            Assign(),
            Number(2),
            Terminal(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());

        Assert.Single(datum.Identifier);

        var unresolved = datum.Type as Type.Unresolved;
        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        var scalar = datum.Initializer as Ronin.Grammar.Literal;
        Assert.Single(scalar?.Tokens.ToArray());
    }

    /*[Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string home = nameof(home);
            const string Building = nameof(Building);
            const string test = nameof(test);

            // var home => shared Building = (2, "test", $7);

            Context module = new()
            {
                new Datum.Declaration
                {
                    Mutability = new Variable(),
                    Identifier = Words(home),
                    Modifiers = new() { Source = new[] { new Global() } },
                    Datatype = Reference(Building),
                    Initializer = new Inputs
                    {
                        new() { value = new Inline { Source = new[] { Number(2) } } },
                        new() { value = new Inline { Source = new[] { Text(test) } } },
                        new() { value = new Inline { Source = new[] { Currency(7) } } }
                    }
                }
            };

            Analyzer analyzer = new();
            module.Parent = analyzer.Global;
            analyzer.Define(module);
            Assert.Empty(analyzer.Errors);

            Assert.Single(module.Members);

            var entry = module.Members.First();
            var identifier = entry.Key;
            var datum = entry.Value as Datum;

            Assert.IsType<Variable>(datum.Mutability);

            Assert.Single(identifier.Source.ToArray());
            Name name = identifier;
            Assert.Single(name.Source.ToArray());
            Assert.Equal(home, name.Source.Span[0].Memory.ToString());

            Assert.IsType<Datatype.Unresolved>(datum.Datatype);
        }
    }*/
}
