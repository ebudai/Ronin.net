using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Function = Ronin.Grammar.Function;

namespace Failure;

[Trait(nameof(Parser), null)]
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

    /*[Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "function scope is part of a module")]
        public void FunctionScopeJoinsModule()
        {
            Context module = new()
            {
                new Function.Declaration
                {
                    Identifier = Identifier("x"),
                    Definition = new() { new Export { Keyword = new PartOf(), Identifier = new() } }
                }
            };

            Analyzer analyzer = new();
            module.Parent = analyzer.Global;
            analyzer.Define(module);
            Assert.Single(analyzer.Errors);

            Error error = analyzer.Errors[0];

            Assert.Equal(Error.Message.ScopeMustBeAnonymous, error.Reason);
        }

        [Fact(DisplayName = "part of conditional scope")]
        public void PartOfConditionalScope()
        {
            Context module = new()
            {
                new ConditionalScope
                {
                    Condition = new Context.Member.Unresolved { Reference = new() { } },
                    Definition = new() { new Export { Keyword = new PartOf(), Identifier = new() } }
                }
            };

            Analyzer analyzer = new();
            analyzer.Define(module);
            Assert.Single(analyzer.Errors);

            Error error = analyzer.Errors[0];

            Assert.Equal(Error.Message.ScopeMustBeAnonymous, error.Reason);
        }

        [Fact(DisplayName = "part of modified scope")]
        public void PartOfModifiedScope()
        {
            Context module = new()
            {
                new AnonymousScope
                {
                    Modifiers = new() { Source = new[] { new Compiled() } },
                    Definition = new() { new Export { Keyword = new PartOf(), Identifier = new() } }
                }
            };

            Analyzer analyzer = new();
            analyzer.Define(module);
            Assert.Single(analyzer.Errors);

            Error error = analyzer.Errors[0];

            Assert.Equal(Error.Message.ScopeMustBeUnmodified, error.Reason);
        }

        [Fact(DisplayName = "part of twice")]
        public void PartOfTwice()
        {
            Context module = new()
            {
                new AnonymousScope
                {
                    Definition = new()
                    { new Export { Keyword = new PartOf(), Identifier = Words("test exporting twice") }, new Export { Keyword = new PartOf(), Identifier = Words("test exporting twice failure") }, }
                }
            };

            Analyzer analyzer = new();
            analyzer.Define(module);
            Assert.Single(analyzer.Errors);

            Error error = analyzer.Errors[0];

            Assert.Equal(Error.Message.ScopeIsAlreadyPartOfModule, error.Reason);
        }
    }*/
}
