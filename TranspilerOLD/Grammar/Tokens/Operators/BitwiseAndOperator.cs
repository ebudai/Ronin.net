using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Operators;

internal class BitwiseAndOperator : Operator
{
    public override Regex[] Regexes { get; } = { new(@"^&", options) };

    protected internal override Precedence Precedence => Precedence.BitwiseAnd;
}
