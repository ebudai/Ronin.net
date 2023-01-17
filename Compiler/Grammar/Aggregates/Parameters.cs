using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Parameters : AggregateSyntax<Parameters, OpenParenthesis, Parameter, Comma, CloseParenthesis> 
{
    public bool Matches(Scalar scalar)
    {
        if (Values.Count is not 1) return false;
        var parameter = Values[0];
        //parameter.



        return default;
    }
}
