using Ronin.Transpiler.Grammar.Flags;
using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Modifiers;

internal class ClassModifier : Modifier
{
    public override Regex[] Regexes { get; } = { new(@"^class\s", options) };

    public override Declaration Modifies => Declaration.Type | Declaration.Member;
}
