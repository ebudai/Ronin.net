namespace Ronin.Transpiler;

[System.Diagnostics.DebuggerDisplay("{Value}")]
internal class Token
{
    public enum Type { Literal, Symbol, Identifier, Keyword }

    public string Value { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public Type Kind { get; set; }

    public static class Codes
    {
        public const string Literal = "L";
        public const string Indentifier = "I";
    }

    public override string ToString() => Kind switch
    {
        Type.Literal => Codes.Literal,
        Type.Symbol => Value,
        Type.Identifier => Codes.Indentifier,
        Type.Keyword => $"<{Value}>",
        _ => throw new ArgumentOutOfRangeException("kind is not in range: " + (int)Kind)
    };    
}

/*internal static class TokenExtensions
{
    public static int IndexOf(this ReadOnlySpan<Token> tokens, string value)
    {
        for (int i = 0, max = tokens.Length; i != max; ++i)
        {
            if (tokens[i].Value == value) return i;
        }
        return -1;
    }

    public static bool IsBefore(this ReadOnlySpan<Token> tokens, string before, string after)
    {
        var beforeindex = tokens.IndexOf(before);
        if (beforeindex is -1) return false;
        var afterindex = tokens.IndexOf(after);
        return afterindex is -1 || beforeindex < afterindex;
    }

    public static int IndexOfMatching(this ReadOnlySpan<Token> tokens, int start, string endSymbol)
    {
        int innerMatchCount = 0;
        string startSymbol = tokens[start].Value;

        if (tokens[start].Kind is not Token.Type.Symbol) throw new Parser.Exception($"cannot match on non-symbol token {startSymbol}");
        
        for (int i = start, max = tokens.Length; i != max; ++i)
        {
            if (tokens[i].Kind is not Token.Type.Symbol) continue;

            if (tokens[i].Value == startSymbol)
            {
                ++innerMatchCount;
            }
            else if (tokens[i].Value == endSymbol)
            {
                if (innerMatchCount is 0) return i;
                --innerMatchCount;
            }
        }

        return -1;
    }
}*/