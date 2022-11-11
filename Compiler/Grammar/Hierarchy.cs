using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Reserved;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Hierarchy : Syntax, IParsable
{
    internal Discriminator Direction { get; private init; }
    internal string[] Name { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        Discriminator? direction = context.Current switch
        {            
            PartOf => Discriminator.Export,
            Import => Discriminator.Import,
            _ => null
        };
        if (direction is null) return null;

        Parser parser = context;
        
        parser.Advance();

        List<string> names = new();
        while (parser.IsNotFinished)
        {
            if (parser.Current is Word word)
            {
                names.Add($"{word}");
            }
            else if (parser.Current is Symbol symbol and not Punctuation)
            {
                names.Add($"{symbol}");
            }
            else if (parser.Current is Text text)
            {
                names.Add($"{text}");
            }
            else
            {
                break;
            }

            parser.Advance();
        }

        if (parser.IsNotFinished)
        {
            if (parser.Current is not Terminal) return ExpectedTerminal.Parse(ref context);
        }

        if (names.Count is 0) return null;

        return new Hierarchy { Name = names.ToArray(), Direction = direction.Value, Source = parser.Commit(ref context) };
    }

    internal enum Discriminator { Import, Export };
}