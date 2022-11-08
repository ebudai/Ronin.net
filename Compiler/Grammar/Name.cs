using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Name : Syntax, IParsable
{
    internal string[] Words { get; private init; }
    internal string[] Hierarchy => string.Join(' ', Words).Split(' ' + Slash.symbol + ' ');

    public static Syntax Parse(ref Parser context)
    {
        List<string> names = new(64);
        Parser parser = context;

        while (parser.IsNotFinished)
        {
            if (parser.Current is Word word)
            {
                names.Add(word.ToString());
            }
            else if (parser.Current is Symbol symbol && CanBeUsedInNames(symbol))
            {
                names.Add(symbol.ToString());
            }
            else if (parser.Current is not Trivium)
            {
                break;
            }

            parser.Advance();
        }

        if (names.Count is 0) return null;

        return new Name { Words = names.ToArray(), Source = parser.Commit(ref context) };
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