using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Name : Syntax, IParsable
{
    internal string[] Names { get; private init; }
    internal string[] Hierarchy => string.Join(' ', Names).Split(" " + Lexicon.Symbols.Hierarchy.character + ' ');

    public static Syntax Parse(Parser parser)
    {
        List<string> names = new();
        int length = 0;
        for (int max = parser.Length; length != max; ++length)
        {
            var lexeme = parser[length];

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
        return names.Count is 0 ? null : new Name { Names = names.ToArray(), Tokens = parser[..length] };
    }
}