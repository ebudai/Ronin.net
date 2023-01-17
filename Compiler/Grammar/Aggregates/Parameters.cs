using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Parameters : AggregateSyntax<Parameters, OpenParenthesis, Parameter, Comma, CloseParenthesis> 
{
    public override string ToString() => '(' + string.Join(",", Values) + ')';
}
