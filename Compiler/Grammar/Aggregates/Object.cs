using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Object : AggregateSyntax<Object, OpenParenthesis, Value, Comma, CloseParenthesis>
{
    public override string ToString() => '(' + string.Join(",", Values) + ')';
}