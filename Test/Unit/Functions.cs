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
            const string cash = nameof(cash);
            const string money = nameof(money);

            // function run home(cash => money) => whole number { return 72; }

            Definition module = new()
            {
                Values = new List<Statement>
                {
                    new Function.Declaration
                    {
                        Identifier = new()
                        {
                            Components = new List<Identifier.Component>
                            {
                                new() { value = new Name { Source = new[] { Word(run), Word(home) } } },
                                new()
                                {
                                    value = new Parameters
                                    {
                                        Values = new List<Datum.Declaration>
                                        {
                                            new()
                                            {
                                                Datatype = new Reference
                                                {
                                                    Components = new List<Reference.Component> { new() { value = new Name { Source = new[] { Word(money) } } } },
                                                    Source = new[] { Word(money) }
                                                },
                                                Mutability = new Variable(),
                                                Name = new() { Source = new[] { Word(cash) } },
                                                Source = new Token[] { Word(cash), Returns(), Word(money) }
                                            }
                                        },
                                        Source = new Token[] { StartValues(), Word(cash), Returns(), Word(money), EndValues() }
                                    }
                                }
                            }
                        },
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
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Empty(errors);

            Assert.Single(module.Children);
            var child = module.Children.First().Value;

            Assert.Single(child.Members);

            var entry = child.Members.First();
            var identifier = entry.Key;
            var function = entry.Value as Function;

            Assert.Equal(5, identifier.value.Source.Length);
            Assert.Equal(cash, identifier.value.Source.Span[1].Memory.ToArray());
            Assert.Equal(money, identifier.value.Source.Span[3].Memory.ToArray());

            Assert.Empty(function.Modifiers.Source.ToArray());

            Assert.IsType<Datatype.Unresolved>(function.Returns);
        }
    }
}
