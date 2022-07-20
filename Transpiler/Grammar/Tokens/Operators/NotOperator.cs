using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Operators;

internal class NotOperator : Operator
{
    public override Regex[] Regexes { get; } = { new("^!", options) };

    protected internal override Precedence Precedence => Precedence.Unary;
}
