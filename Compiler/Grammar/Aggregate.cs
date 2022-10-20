using Ronin.Compiler;
using Ronin.Token.Symbols;

namespace Ronin.Grammar;

internal class Aggregate : Syntax, IParsable
{
    internal Reference[] Parameters { get; init; }

    internal Aggregate(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        if (parser[0] is not OpenParenthesis) return null;

        int length = OpenParenthesis.character.ToString().Length;
        List<Reference> references = new();        
        while (length < parser.Length)
        {
            Parser attempt = new(parser, length);
            var parsed = Reference.Parse(attempt);
            if (parsed is null) return null;
            if (parsed is Unexpected unexpected) return unexpected;
            //if (attempt.IsEmpty || attempt[0] is not CloseParenthesis and not Separator) return new Expected<Separator, CloseParenthesis>(attempt);
            references.Add(parsed as Reference);
            length += attempt.Cursor;
        }

        return new Aggregate(parser, length - OpenParenthesis.character.ToString().Length) { Parameters = references.ToArray() };
    }
}