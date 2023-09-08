using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Hierarchy;
using Ronin.Lexicon;
using Test;

using Datatype = Ronin.Grammar.Datatype;
using Function = Ronin.Grammar.Function;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Functions : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // function test(x => number) { return 7; }

        List<Token> tokens = new()
        {
            Keyword.Function(),
            Word("test"),
            StartValues(),
            Word("x"),
            Returns(),
            Word("number"),
            EndValues(),
            StartScope(),
            Word("return"),
            Number(7),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = Function.Declaration.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Components.Count);

        Assert.Equal(1, function.Identifier.Components[0].Source.Length);
        
        Parameters parameters = function.Identifier.Components[1];

        Assert.Single(parameters);
        var parameter = parameters[0];
        Assert.Equal(1, parameter?.Identifier?.Source.Length);

        Assert.Single(parameter.Datatype?.Components);
        Name type = parameter.Datatype.Components[0];
        Assert.Equal(1, type?.Source.Length);
        
        Assert.Single(function.Definition);
        var line = function.Definition[0] as Reference.Unresolved;
        var unresolved = line?.Member as Context.Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Components.Count);

        Name @return = unresolved.Reference.Components[0];
        Assert.Equal(1, @return?.Source.Length);

        Value.Anonymous scalar = unresolved.Reference.Components[1];
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Fact(DisplayName = "specifies return datatype")]
    public void ReturnsSymbol()
    {
        // function test(x => text) => number { return x as number; }

        List<Token> tokens = new()
        {
            Keyword.Function(),
            Word("test"),
            StartValues(),
            Word("x"),
            Returns(),
            Word("text"),
            EndValues(),
            Returns(),
            Word("number"),
            StartScope(),
            Word("return"),
            Word("x"),
            Word("as"),
            Word("number"),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = Function.Declaration.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Components?.Count);

        Assert.Equal(1, function.Identifier.Components[0].Source.Length);

        Parameters parameters = function.Identifier.Components[1];
        Assert.Single(parameters);
        var parameter = parameters[0];
        Assert.Equal(1, parameter.Identifier?.Source.Length);

        Assert.Single(parameter.Datatype?.Components);
        Name type = parameter.Datatype.Components[0];
        Assert.Equal(1, type?.Source.Length);        

        Assert.Single(function.Returns?.Components);
        Name returns = function.Returns.Components[0];
        Assert.Equal(1, returns?.Source.Length);

        Assert.Single(function.Definition);
        var line = function.Definition[0] as Reference.Unresolved;
        var unresolved = line?.Member as Context.Member.Unresolved;
        Assert.Single(unresolved?.Reference.Components);
        Name @return = unresolved.Reference.Components[0];
        Assert.Equal(4, @return?.Source.Length);
    }

    [Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string run = nameof(run);
            const string home = nameof(home);
            const string whole = nameof(whole);
            const string number = nameof(number);
            const string @return = nameof(@return);
            const string cash = nameof(cash);
            const string money = nameof(money);

            // function run home (cash => money) => whole number { return 72; }

            AnonymousScope scope = new()
            {
                Definition = new()
                {
                    new Function.Declaration
                    {
                        Identifier = new()
                        {
                            Components = new List<Identifier.Component>
                            {
                                Name(run, home),
                                new Parameters
                                {
                                    new()
                                    {
                                        Datatype = Reference(money),
                                        Mutability = new Constant(),
                                        Identifier = Words(cash),
                                        Source = new Token[] { Word(cash), Returns(), Word(money) }
                                    },
                                    //Source = new Token[] { StartValues(), Word(cash), Returns(), Word(money), EndValues() }
                                }
                            }
                        },
                        Returns = Reference(whole, number),
                        Definition = new()
                        {
                            UnresolvedReference(@return),
                            new Inline { Source = new[] { Number(72) } }
                        }
                    }
                }
            };

            Analyzer analyzer = new();
            analyzer.DefineScope(analyzer.Global, scope);
            Assert.Empty(analyzer.Errors);

            Assert.Single(analyzer.Global.Contexts);
            var context = analyzer.Global.Contexts.First();

            Assert.Single(context.Members);

            var entry = context.Members.First();
            var identifier = entry.Key;
            var function = entry.Value as Function;

            Assert.Equal(2, identifier.Components.Count);
            Assert.Equal(2, identifier.Components[0].Source.Length);
            Assert.Equal(run, identifier.Components[0].Source.Span[0].Memory.ToArray());
            Assert.Equal(home, identifier.Components[0].Source.Span[1].Memory.ToArray());

            Parameters parameters = identifier.Components[1];
            Assert.Single(parameters);
            Assert.Single(parameters[0].Identifier.Components);
            Name name = parameters[0].Identifier.Components[0];
            Assert.Single(name?.Source.ToArray());
            Assert.Equal(cash, name.Source.Span[0].Memory.ToArray());
            Assert.Single(parameters[0].Datatype.Components);
            name = parameters[0].Datatype.Components[0];
            Assert.Single(name?.Source.ToArray());
            Assert.Equal(money, name.Source.Span[0].Memory.ToArray());
            Assert.Empty(function.Modifiers.Source.ToArray());

            Assert.IsType<Datatype.Unresolved>(function.Returns);
        }
    }

    [Trait(nameof(Analyzer), nameof(Resolution))]
    public class Resolution : AnalysisTests
    {
        [Fact(DisplayName = "overloaded")]
        public void Overloaded()
        {
            const string test = nameof(test);
            const string x = nameof(x);

            // function test(x => number) { }
            // function test(x => money) { }
            // test 3;

            Analyzer analyzer = new();

            {
                Parameters param = new();
                Identifier id = Identifier(x);
                param.Data.Add(id, new Datum { Datatype = new() });
                analyzer.Global.Add(Identifier(Name(test), param), new Function());
            }

            {
                Parameters param = new();
                Identifier id = Identifier(x);
                param.Data.Add(id, new Datum { Datatype = new() });
                analyzer.Global.Add(Identifier(Name(test), param), new Function());
            }

            Reference.Component parameter = new() { value = new Inline { Source = new[] { Number(3) } }, Source = new[] { Number(3) } };
            var reference = Reference(Name(test), parameter);

            Reference.Unresolved unresolved = new() { Member = new Function.Unresolved { Reference = reference } };
            analyzer.Global.Add(unresolved);

            //analyzer.Resolve(Module.Global, errors);

            var overloaded = unresolved.Member as Function.Overloaded;
            Assert.Equal(2, overloaded?.Overloads.Count);
        }
    }
}
