using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Tokens;

internal class Unknown : Token
{
    // executes the regex, finds the pattern in Value,
    // generates a new token from the find, then splits
    // up value into either [new token, new unknown],
    // [new unknown, new token, new unknown], or 
    // [new unknown, new token], depending on where the match is
    public Token[] Split<T>(Match match) where T : Token, new()
    {
        T token = new() { Value = match.Value };
        
        if (match.Index is 0) // match is at the beginning of Value
        {
            return new Token[] { token, new Unknown { Value = this.Value[match.Index..] } };
        }

        if (match.Index + match.Value.Length == Value.Length) // match is at end of value
        {
            return new Token[] { new Unknown { Value = this.Value[..match.Index] }, token };
        }

        // match is in the middle
        var end = match.Index + match.Value.Length;
        return new Token[] { new Unknown { Value = this.Value[..match.Index] }, token, new Unknown { Value = this.Value[end..] } };
    }
}
