using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Arguments : Aggregate<Arguments, OpenParenthesis, Value, Separator, CloseParenthesis>
{
    //public override string ToString() => '(' + string.Join(",", Values) + ')';
}