using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords.Primitives;

internal class SbytePrimitive : Primitive
{
    public override LexicalScope Applies => LexicalScope.All;

    public override Regex[] Regexes { get; } = { new(@"^sbyte\s", options) };
}
