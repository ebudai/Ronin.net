using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Name : Syntax, IParsable
{
    internal string[] Names { get; private init; }

    private Name(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        List<string> names = new();
        int tokensConsumed = 0;
        for (int max = parser.Length; tokensConsumed != max; ++tokensConsumed)
        {
            var lexeme = parser[tokensConsumed];

            if (lexeme is Word word)
            {
                names.Add(word.ToString());
            }
            else if (lexeme is Symbol symbol && symbol.CanBeUsedInNames)
            {
                names.Add(symbol.ToString());
            }
            else if (lexeme is not Whitespace and not Comment)
            {
                break;
            }
        }
        return names.Count is 0 ? null : new Name(parser, tokensConsumed) { Names = names.ToArray() };
    }
}