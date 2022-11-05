using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Name : Syntax, IParsable
{
    internal string[] Words { get; private init; }
    internal string[] Hierarchy => string.Join(' ', Words).Split(" " + Lexicon.Symbols.Hierarchy.character + ' ');

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        List<string> names = new();
        while (parser.IsNotEmpty)
        {
            ref readonly var token = ref parser[0];
            
            if (token is Word word)
            {
                names.Add(word.ToString());
            }
            else if (token is Symbol { CanBeUsedInNames: true } symbol)
            {
                names.Add(symbol.ToString());
            }
            else if (token is not Trivium)
            {
                break;
            }

            ++parser.Cursor;
        }

        return names.Count is 0 ? null : new Name { Words = names.ToArray(), Tokens = parser.GetTokens(ref context) };
    }
}