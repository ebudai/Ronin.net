using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Reserved;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Hierarchy : Syntax, IParsable
{
    internal Keyword Direction { get; private init; }
    internal List<string> Name { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        Keyword direction = context.Current is PartOf or Import ? context.Current as Keyword : null;
        if (direction is null) return null;

        Parser parser = context;

        parser.Advance();

        List<string> names = new();
        while (parser.IsNotFinished)
        {
            if (parser.Current is Word or Symbol and not Punctuation)
            {
                names.Add($"{parser.Current}");
            }
            else if (parser.Current is Text text)
            {
                names.Add(text.Value);
            }
            else if (parser.Current is not Terminal)
            {
                return ExpectedTerminalError.Parse(ref context);
            }
            else
            {
                break;
            }

            parser.Advance();
        }

        if (names.Count is 0) return null;

        return new Hierarchy { Name = names, Direction = direction, Source = parser.Commit(ref context) };
    }   
}