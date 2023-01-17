using Ronin.Compiler;
using Ronin.Grammar.Unions;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Aggregate : AggregateElementSyntax<Aggregate, OpenParenthesis, Value, Comma, CloseParenthesis>
{

}