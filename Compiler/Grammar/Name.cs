using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Name : Syntax, IParsable
{
    internal string[] Words { get; private init; }
    internal string[] Hierarchy => string.Join(' ', Words).Split(" " + Lexicon.Symbols.Slash.character + ' ');

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
            else if (token is Symbol symbol && CanBeUsedInNames(symbol))
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

    private static bool CanBeUsedInNames(Symbol symbol) => symbol 
        is not Open 
        and not Close 
        and not Returns 
        and not Separator 
        and not Terminal
        and not Assign
        and not TextDelimiter;
}