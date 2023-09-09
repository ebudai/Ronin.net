using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Function = Ronin.Grammar.Function;

namespace Failure;

[Trait(nameof(Parser), null)]
public class Unknowns : ParsingTests
{
    [Fact(DisplayName = "unknown")]
    public void UnknownSyntax()
    {
        // datatype => ;

        List<Token> tokens = new()
        {
            Keyword.Datatype(),
            Returns(),
            Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse().ToList();
        
        Assert.Single(statements);
        Assert.IsType<Unknown>(statements[0]);
    }

    [Trait(nameof(Analyzer), nameof(Declaration))]
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

            Analyzer analyzer = new();
            analyzer.Define(module);
            Assert.Single(analyzer.Errors);

            var error = analyzer.Errors[0];
            Assert.Equal(Error.Message.UnknownSyntax, error.Reason);
        }
    }
}
