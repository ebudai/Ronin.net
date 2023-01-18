using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Index : AggregateSyntax<Index, OpenSquareBracket, Value, Separator, CloseSquareBracket>
{
    public override string ToString() => '[' + string.Join(",", Values) + ']';
}
