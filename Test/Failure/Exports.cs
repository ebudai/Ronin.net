using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Function = Ronin.Grammar.Function;

namespace Failure;

[Trait("Parser", null)]
public class Exports : ParsingTests
{
    [Fact(DisplayName = "missing identifier")]
    public void MissingIdentifier() 
    {
        // part of ;

        List<Token> tokens = new()
        {
            Keyword.PartOf(),
            Terminal(),
        };

        Parser parser = new(tokens);
        var export = Export.Parse(ref parser);

        Assert.Null(export);
    }

    [Trait("Analyzer", "declaration")]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "function scope is part of a module")]
        public void FunctionScopeJoinsModule()
        {
            Definition module = new()
            {
                Values = new List<Statement>
                {
                    new Function.Declaration
                    {
                        Identifier = Identifier("x"),
                        Definition = new() { Values = new() { new Export { Keyword = new PartOf(), Identifier = new() } } }
                    }
                }
            };

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Single(errors);

            Error error = errors[0];

            Assert.Equal(Error.Message.ScopeMustBeAnonymous, error.Reason);
        }

        [Fact(DisplayName = "part of conditional scope")]
        public void PartOfConditionalScope()
        {
            Definition module = new()
            {
                Values = new List<Statement>
                {
                    new ConditionalScope
                    {
                        Condition = new Value.Unresolved { Reference = new() { } },
                        Definition = new() { Values = new() { new Export { Keyword = new PartOf(), Identifier = new() } } }
                    }
                }
            };

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Single(errors);

            Error error = errors[0];

            Assert.Equal(Error.Message.ScopeMustBeAnonymous, error.Reason);
        }

        [Fact(DisplayName = "part of modified scope")]
        public void PartOfModifiedScope()
        {
            Definition module = new()
            {
                Values = new List<Statement>
                {
                    new AnonymousScope
                    {
                        Modifiers = new() { Source = new[] { new Compiled() } },
                        Definition = new() { Values = new() { new Export { Keyword = new PartOf(), Identifier = new() } } }
                    }
                }
            };

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Single(errors);

            Error error = errors[0];

            Assert.Equal(Error.Message.ScopeMustBeUnmodified, error.Reason);
        }

        [Fact(DisplayName = "part of twice")]
        public void PartOfTwice()
        {
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
                                new Export { Keyword = new PartOf(), Identifier = Words("test exporting twice") },
                                new Export { Keyword = new PartOf(), Identifier = Words("test exporting twice failure") },
                            }
                        }
                    }
                }
            };

            Global.Scope.Children.Clear();

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Single(errors);

            Error error = errors[0];

            Assert.Equal(Error.Message.ScopeIsAlreadyPartOfModule, error.Reason);
        }
    }
}
