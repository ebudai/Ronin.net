using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Parameters : AggregateSyntax<Parameters, OpenParenthesis, Argument, Comma, CloseParenthesis> 
{
    public bool Matches(Scalar scalar)
    {
        if (Values.Length is not 1) return false;
        var parameter = Values[0];
        //parameter.



        return default;
    }
}
