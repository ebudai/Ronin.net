using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Name : Syntax, IParsable
{
    internal List<string> Words { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        List<string> names = new(64);
        Parser parser = context;

        while (parser.IsNotFinished)
        {
            var name = parser.Current;
            
            if (name is Word or Symbol and not Punctuation)
            {
                names.Add(name);
            }
            else
            {
                break;
            }

            parser.Advance();
        }

        if (names.Count is 0) return null;

        return new Name { Words = names, Source = parser.Commit(ref context) };
    }

    //public override string ToString() => string.Join("_", Words);
}