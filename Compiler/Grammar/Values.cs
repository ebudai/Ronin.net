using Ronin.Compiler;
using Ronin.Grammar.Unions;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Values : AggregateSyntax<Values, OpenParenthesis, Value, Comma, CloseParenthesis>
{

}