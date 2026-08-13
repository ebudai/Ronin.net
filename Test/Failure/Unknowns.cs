using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Function = Ronin.Grammar.Function;

namespace Integration;

[Trait(nameof(Parser), null)]
public class Unknowns : ParsingTests
{
    [Fact(DisplayName = "unknown")]
    public void UnknownSyntax()
    {
        // => ;

        List<Token> tokens = new()
        {
            Arrow(),
            Terminal(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var module = parser.Parse();
        
        Assert.Single(module.Scopes);
        Assert.Single(module.Scopes[0].Statements);
        Assert.IsType<Unknown>(module.Scopes[0].Statements[0]);
    }

    /*[Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "inside definition")]
        public void InsideDefinition()
        {
            var function = new Function.Declaration
            {
                Identifier = Words("unknown function"),
                Definition = new() { new Unknown() }
            };

            Context module = new() { function };

            Analyzer analyzer = new() { Global = module };
            analyzer.Define();
            Assert.Single(analyzer.Errors);

            var error = analyzer.Errors[0];
            Assert.Equal(Error.Message.UnknownSyntax, error.Reason);
        }
    }*/
}
