using Ronin.Transpiler.Grammar.Flags;
using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Modifiers;

internal class ConstModifier : Modifier
{
    public override Regex[] Regexes { get; } = { new(@"^const\s", options) };

    public override Declaration Modifies => Declaration.All;
}
