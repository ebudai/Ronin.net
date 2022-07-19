using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens;

internal sealed class Whitespace : Token
{
    public Whitespace() : base() { }

    public string Spaces { get; private set; } = string.Empty;

    public override string ToString() => Spaces;   
}
