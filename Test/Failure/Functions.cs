using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Function = Ronin.Grammar.Function;

namespace Failure;

[Trait(nameof(Parser), null)]
public class Functions : ParsingTests
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        // function { }

        List<Token> tokens = new()
        {
            Keyword.Function(),
            StartScope(),
            EndScope(),
        };

        Parser parser = new(tokens);
        var function = Function.Declaration.Parse(ref parser);
        
        Assert.Null(function);
    }

    [Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "redefinition")]
        public void Redefinition()
        {
            const string name = "best ever";

            Context module = new()
            {
                new Function.Declaration
                {
                    Identifier = Words(name),
                    Definition = new()
                },
                new Function.Declaration
                {
                    Identifier = Words(name),
                    Definition = new()
                }
            };

            Analyzer analyzer = new();
            analyzer.Define(module);
            Assert.Single(analyzer.Errors);
            Assert.Equal(Error.Message.Redefinition, analyzer.Errors[0].Reason);
        }
    }
}
