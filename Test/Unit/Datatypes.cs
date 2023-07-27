using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Datatype = Ronin.Grammar.Datatype;

namespace Unit;

[Trait("Parser", null)]
public class Datatypes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // datatype Test { }

        List<Token> tokens = new()
        {
            Keyword.Datatype(),
            Word("Test"),
            StartScope(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = Datatype.Declaration.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Identifier.Component name = datatype.Identifier.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        // datatype Algebra Example = number or { var cash => money; var debt => money; }

        List<Token> tokens = new()
        {
            Keyword.Datatype(),
            Word("Algebra"),
            Word("Example"),
            Assign(),
            Word("number"),
            Word("or"),
            StartScope(),
            Keyword.Variable(),
            Word("cash"),
            Returns(),
            Word("money"),
            Terminal(),
            Keyword.Variable(),
            Word("debt"),
            Returns(),
            Word("money"),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = Datatype.Declaration.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Name algebra = datatype.Algebra.Components[0];
        Assert.Equal(2, algebra?.Source.Length);

        Assert.Equal(2, datatype.Definition?.Values.Count);

        {
            var cash = datatype.Definition.Values[0] as Datum.Declaration;
            Assert.IsType<Variable>(cash?.Mutability);
            Assert.Equal(1, cash.Name?.Source.Length);
            Assert.Single(cash.Datatype?.Components);
            Name type = cash.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        {
            var debt = datatype.Definition.Values[1] as Datum.Declaration;
            Assert.IsType<Variable>(debt?.Mutability);
            Assert.Equal(1, debt.Name?.Source.Length);
            Assert.Single(debt.Datatype?.Components);
            Name type = debt.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }
    }

    [Trait("Analyzer", "declaration")]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string Big = nameof(Big);
            const string text = nameof(text);
            const string or = nameof(or);
            const string x = nameof(x);
            const string number = nameof(number);

            // datatype Big = text or { var x => number; }

            Definition module = new()
            {
                Values = new List<Statement>
                {
                    new Datatype.Declaration
                    {
                        Identifier = new Name { Source = new[] { Word(Big) } },
                        Algebra = new Reference
                        {
                            Components = new List<Reference.Component>
                            {
                                new() { value = new Name { Source = new[] { Word(text) } } },
                                new() { value = new Name { Source = new[] { Word(or) } } },
                            }
                        },
                        Definition = new()
                        {
                            Values = new List<Statement>
                            {
                                new Datum.Declaration
                                {
                                    Mutability = new Variable(),
                                    Name = new() { Source = new[] { Word(x) } },
                                    Datatype = new Reference
                                    {
                                        Components = new List<Reference.Component>
                                        {
                                            new() { value = new Name { Source = new[] { Word(number) } } },
                                        }
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

            Assert.Single(module.Members);

            var entry = module.Members.First();
            var identifier = entry.Key;
            var datatype = entry.Value as Datatype;

            Assert.Single(identifier.value.Source.ToArray());
            Assert.Equal(Big, identifier.value.Source.Span[0].Memory.ToArray());

            var algebra = datatype.Algebra as Algebra.Unresolved;
            Assert.Equal(2, algebra?.Reference.Components.Count);
            Assert.Single(algebra.Reference.Components[0].value.Source.ToArray());
            Assert.Equal(text, algebra.Reference.Components[0].value.Source.Span[0].Memory.ToArray());
            Assert.Single(algebra.Reference.Components[1].value.Source.ToArray());
            Assert.Equal(or, algebra.Reference.Components[1].value.Source.Span[0].Memory.ToArray());

            Assert.Single(datatype.Definition.Members);

            var datumentry = datatype.Definition.Members.First();
            var name = datumentry.Key;
            var datum = datumentry.Value as Datum;

            Assert.Single(name.value.Source.ToArray());
            Assert.Equal(x, name.value.Source.Span[0].Memory.ToArray());

            Assert.IsType<Variable>(datum?.Mutability);
            var unresolved = datum.Datatype as Datatype.Unresolved;
            Assert.Single(unresolved?.Reference.Components);
            var unresolvedname = unresolved.Reference.Components[0].value as Name;
            Assert.Single(unresolvedname.Source.ToArray());
            Assert.Equal(number, unresolvedname.Source.Span[0].Memory.ToArray());
        }
    }
}
