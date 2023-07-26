using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Datatype = Ronin.Grammar.Datatype;
using Function = Ronin.Grammar.Function;

namespace Unit;

[Trait("Parser", null)]
public class Functions : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        /*
         *      
         *      function test(x => number) { return 7; }
         *      
         */

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

        {
            Name name = function.Identifier.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Parameters parameters = function.Identifier.Components[1];
            
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Equal(1, parameter?.Name?.Source.Length);

            Assert.Single(parameter.Datatype?.Components);
            Name type = parameter.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        Assert.Single(function.Definition?.Values);
        var line = function.Definition.Values[0] as Reference;
            
        Assert.Equal(2, line?.Components?.Count);

        {
            Name @return = line.Components[0];
            Assert.Equal(1, @return?.Source.Length);
        }

        {
            AnonymousValue scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
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

        {
            Name name = function.Identifier.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Parameters parameters = function.Identifier.Components[1];
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Equal(1, parameter.Name?.Source.Length);

            Assert.Single(parameter.Datatype?.Components);
            Name type = parameter.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        Assert.Single(function.Returns?.Components);
        Name returns = function.Returns.Components[0];
        Assert.Equal(1, returns?.Source.Length);

        Assert.Single(function.Definition?.Values);
        var line = function.Definition.Values[0] as Reference;
        Assert.Single(line?.Components);
        Name @return = line.Components[0];
        Assert.Equal(4, @return?.Source.Length);
    }

    [Trait("Analyzer", "declaration")]
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

            // function run home => whole number { return 72; }

            Module module = new()
            {
                Values = new List<Statement>
                {
                    new Function.Declaration
                    {
                        Identifier = new Name { Source = new[] { Word(run), Word(home) } },
                        Returns = new Reference
                        {
                            Components = new List<Reference.Component>
                            {
                                new() { value = new Name { Source = new[] { Word(whole), Word(number) } } },
                            }
                        },
                        Definition = new()
                        {
                            Values = new List<Statement>
                            {
                                new Reference
                                {
                                    Components = new List<Reference.Component>
                                    {
                                        new() { value = new Name { Source = new[] { Word(@return) } } },
                                        new() { value = new Inline { Source = new[] { Number(72) } } },
                                    }
                                }
                            }
                        }
                    }
                }
            };

            List<Error> errors = new();
            Analyzer.Define(Module.Main, module, errors);
            Assert.Empty(errors);

            Assert.Single(module.Elements);

            var entry = module.Elements.First();
            var identifier = entry.Key;
            var function = entry.Value as Function;

            Assert.Equal(2, identifier.value.Source.Length);
            Assert.Equal(run, identifier.value.Source.Span[0].Memory.ToArray());
            Assert.Equal(home, identifier.value.Source.Span[1].Memory.ToArray());

            Assert.Null(function.Modifiers);

            Assert.IsType<Datatype.Unresolved>(function.Returns);


        }
    }
}
