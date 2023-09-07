using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Hierarchy;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class AnonymousScopes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string @return = nameof(@return);

        // { return 2; }

        List<Token> tokens = new()
        {
            StartScope(),
            Word(@return),
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

    [Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string x = nameof(x);

            // { var x = 3; }

            AnonymousScope scope = new()
            {
                Definition = new()
                {
                    new Datum.Declaration
                    {
                        Mutability = new Variable(),
                        Identifier = Words(x),
                        Initializer = new Inline { Source = new[] { Number(3) } }
                    }
                }
            };
            
            Analyzer analyzer = new();
            scope.Definition.Parent = analyzer.Global;
            analyzer.Define(scope.Definition);
            Assert.Empty(analyzer.Errors);

            Assert.Single(scope.Definition.Members);
            var datum = scope.Definition.Members.First().Value;
            Assert.IsAssignableFrom<Datum>(datum);
        }

        [Fact(DisplayName = "inner scope")]
        public void Inner()
        {
            const string x = nameof(x);

            // { { var x = 3; } }

            Context module = new()
            {
                new AnonymousScope
                {
                    Definition = new()
                    {
                        new AnonymousScope
                        {
                            Definition = new()
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

            Analyzer analyzer = new();
            module.Parent = analyzer.Global;
            analyzer.Define(module);
            Assert.Empty(analyzer.Errors);

            Assert.Single(module);
            var scope = module[0] as AnonymousScope;
            Assert.NotNull(scope);
            Assert.Single(scope.Definition);
            var inner = scope.Definition[0] as AnonymousScope;
            Assert.NotNull(inner);
            Assert.Single(inner.Definition.Members);
            var datum = inner.Definition.Members.First().Value;
            Assert.IsAssignableFrom<Datum>(datum);
        }
    }
}