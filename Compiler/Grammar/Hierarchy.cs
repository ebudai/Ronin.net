using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Reserved;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Hierarchy : Syntax, IParsable
{
    public Keyword Direction { get; init; }
    public List<string> Name { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Keyword direction = context.Current is PartOf or Import ? context.Current as Keyword : null;
        if (direction is null) return null;

        Parser parser = context;

        parser.Advance();

        List<string> names = new();
        while (parser.IsNotFinished)
        {
            var token = parser.Current;
            if (token is Word or Symbol and not Punctuation)
            {
                names.Add(token);
            }
            else if (token is Punctuation and not Terminal)
            {
                return UnexpectedSymbolError.Parse(ref context);
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

    public override string ToString()
    {
        var code = Direction switch
        {
            PartOf => "namespace ",
            Import => "using ",
            _ => string.Empty,
        };
        return code + string.Join(".", Name);
    }
}