using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Hierarchy
{
    internal static (string[], int) Parse(Parser parser)
    {
        List<string> hierarchy = new() { string.Empty };
        int tokensConsumed = 1;
        for (int max = parser.Length; tokensConsumed != max; ++tokensConsumed)
        {
            if (parser[tokensConsumed] is Whitespace) continue;

            if (parser[tokensConsumed] is Symbol symbol && symbol.IsTerminal) break;

            string text;
            if (parser[tokensConsumed] is Name name) text = name.ToString();
            else if (parser[tokensConsumed] is Keyword word) text = word.ToString();
            else return (null, tokensConsumed);

            var names = text.Split(Symbol.hierarchy);
            if (hierarchy[^1].Length is not 0) hierarchy[^1] += ' ';
            hierarchy[^1] += names[0];
            if (names.Length is > 1) hierarchy.AddRange(names[1..]);
        }

        var array = hierarchy.Count is 1 && hierarchy[0].Length is 0 ? null : hierarchy.ToArray();
        return (array, tokensConsumed + 1); // one extra for the terminal
    }
}
