using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class PartOf : Syntax, IParsable
{
    internal PartOf(Parser parser, int length) : base(parser, length) { }

    internal string[] Name { get; init; }

    public static Syntax Parse(Parser parser)
    {
        if (parser[0] is not Lexicon.Reserved.PartOf) return null;

        Parser attempt = new(parser, 1);
        var parsed = Grammar.Name.Parse(attempt);

        if (parsed is Error error) return error;

        if (parsed is Name name && attempt[0] is Terminal)
        {
            PartOf partof = new(parser, attempt.Cursor + 1 /* for Terminal token */) { Name = name.Hierarchy };
            parser.Cursor = attempt.Cursor + 1; // + 1 for Terminal token
            return partof;
        }

        return Error.Parse(attempt);
    }
}
  