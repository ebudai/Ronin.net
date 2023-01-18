using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Object : AggregateSyntax<Object, OpenParenthesis, Value, Separator, CloseParenthesis>
{
    //public override string ToString() => '(' + string.Join(",", Values) + ')';
}