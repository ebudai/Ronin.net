using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Parameters : Aggregate<Parameters, OpenParenthesis, Parameter, Separator, CloseParenthesis> 
{
    //public override string ToString() => '(' + string.Join(",", Values) + ')';
}
