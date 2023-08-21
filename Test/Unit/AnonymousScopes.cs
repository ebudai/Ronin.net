using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class AnonymousScopes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // { return 2; }

        List<Token> tokens = new()
        {
            StartScope(),
            Word("return"),
            Number(2),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var anonymous = AnonymousScope.Parse(ref parser);

        Assert.NotNull(anonymous.Definition);
    }

    [Fact(DisplayName = "compiled")]
    public void Compiled()
    {
        // { run to the store; }

        List<Token> tokens = new()
        {
            StartScope(),
            Word("run"),
            Word("to"),
            Word("the"),
            Word("store"),
            Terminal(),
            EndScope(),
            Sentinel.Instance,
        };

        Parser parser = new(tokens);
        var anonymous = AnonymousScope.Parse(ref parser);

        Assert.NotNull(anonymous.Definition);
    }

    [Trait("Analyzer", "declaration")]
    public class Delcaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string x = nameof(x);

            // { var x = 3; }

            Definition module = new()
            {
                Values = new List<Statement>
                {
                    new AnonymousScope
                    {
                        Definition = new()
                        {
                            Values = new()
                            {
                                new Datum.Declaration
                                {
                                    Mutability = new Variable(),
                                    Identifier = Words(x),
                                    Initializer = new Inline { Source = new[] { Number(3) } }
                                }
                            }
                        }
                    }
                }
            };

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Empty(errors);

            Assert.Single(module.Statements);
            var scope = module.Statements[0] as AnonymousScope;
            Assert.NotNull(scope);
            Assert.Single(scope.Definition.Members);
            var datum = scope.Definition.Members.First().Value;
            Assert.IsAssignableFrom<Datum>(datum);
        }

        [Fact(DisplayName = "inner scope")]
        public void Inner()
        {
            const string x = nameof(x);

            // { { var x = 3; } }

            Definition module = new()
            {
                Values = new List<Statement>
                {
                    new AnonymousScope
                    {
                        Definition = new()
                        {
                            Values = new()
                            {
                                new AnonymousScope
                                {
                                    Definition = new()
                                    {
                                        Values = new()
                                        {
                                            new Datum.Declaration
                                            {
                                                Mutability = new Variable(),
                                                Identifier = Words(x),
                                                Initializer = new Inline { Source = new[] { Number(3) } }
                                            }
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

            Assert.Single(module.Statements);
            var scope = module.Statements[0] as AnonymousScope;
            Assert.NotNull(scope);
            Assert.Single(scope.Definition.Statements);
            var inner = scope.Definition.Statements[0] as AnonymousScope;
            Assert.NotNull(inner);
            Assert.Single(inner.Definition.Members);
            var datum = inner.Definition.Members.First().Value;
            Assert.IsAssignableFrom<Datum>(datum);
        }
    }
}