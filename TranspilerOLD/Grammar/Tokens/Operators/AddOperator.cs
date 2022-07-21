using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Operators; 

internal class AddOperator : Operator
{
    public override Regex[] Regexes { get; } = { new(@"^[+]", options) }; //TODO same as unary plus!  subtract same problem with unary minus

    protected internal override Precedence Precedence => Precedence.Additive;
}
