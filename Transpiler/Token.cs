namespace Ronin.Transpiler;

[System.Diagnostics.DebuggerDisplay("{Value}")]
internal class Token
{
    public enum Type { Literal, Symbol, Identifier }

    public string Value { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public Type Kind { get; set; }

    public override string ToString() => Value;
}

internal static class TokenExtensions
{
    public static int IndexOf(this ReadOnlySpan<Token> tokens, string value)
    {
        for (int i = 0, max = tokens.Length; i != max; ++i)
        {
            if (tokens[i].Value == value) return i;
        }
        return -1;
    }
}