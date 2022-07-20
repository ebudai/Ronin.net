using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords.Primitives;

internal class BoolPrimitive : Primitive
{
    public override LexicalScope Applies => LexicalScope.All;

    public override Regex[] Regexes { get; } = { new(@"^bool\s", options) };
}
