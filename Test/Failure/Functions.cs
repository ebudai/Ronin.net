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

    [Trait(nameof(Analyzer), "declaration")]
    public class Declaration : AnalysisTests
    {
        [Fact(DisplayName = "redefinition")]
        public void Redefinition()
        {
            const string name = "best ever";

            Definition module = new()
            {
                Values = new List<Statement>
                {
                    new Function.Declaration
                    {
                        Identifier = new(Words(name)),
                        Definition = new() { Values = new() }
                    },
                    new Function.Declaration
                    {
                        Identifier = new(Words(name)),
                        Definition = new() { Values = new() }
                    }
                }
            };

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Single(errors);
            Assert.Equal(Error.Message.Redefinition, errors[0].Reason);
        }
    }
}
