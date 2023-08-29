using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Hierarchy;
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
        var statements = parser.Parse().Values;
        
        Assert.Single(statements);
        Assert.IsType<Unknown>(statements[0]);
    }

    [Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "inside definition")]
        public void InsideDefinition()
        {
            Context module = new()
            {
                Values = new List<Statement>
                {
                    new Function.Declaration
                    {
                        Identifier = Words("unknown function"),
                        Definition = new()
                        {
                            Values = new List<Statement>
                            {
                                new Unknown()
                            }
                        }
                    }
                }
            };

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Single(errors);

            var error = errors[0];
            Assert.Equal(Error.Message.UnknownSyntax, error.Reason);
        }
    }
}
