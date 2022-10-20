using Ronin.Compiler;
using Ronin.Token;
using Ronin.Token.Symbols;

namespace Ronin.Grammar;

internal class PartOf : Syntax, IParsable
{
    internal PartOf(Parser parser, int length) : base(parser, length) { }

    internal string[] Name { get; init; }

    public static Syntax Parse(Parser parser)
    {
        if (parser[0] is not Token.Keywords.PartOf) return null;

        int tokensConsumed = 2;
        List<string> names = new() { string.Empty };
        for (int max = parser.Length; tokensConsumed != max; ++tokensConsumed)
        {
            var lexeme = parser[tokensConsumed];
            if (lexeme is Comment or Whitespace) continue;
            else if (lexeme is Terminal) break;
            else if (lexeme is Word word) names[^1] += (names[^1] is "" ? "" : " ") + word;
            else if (lexeme is Hierarchy) names.Add(string.Empty);
            else return new Expected<Word, Hierarchy>(parser);
        }
        
        return new PartOf(parser, tokensConsumed + 1) { Name = names.ToArray() };
    }
}
 