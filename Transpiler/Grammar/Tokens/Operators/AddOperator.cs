using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Operators; 

internal class AddOperator : Operator
{
    public override Regex[] Regexes { get; } = { new(@"^[+]", options) };

    protected internal override Precedence Precedence => Precedence.Additive;
}
