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

        int length = parser[0].Length;
        List<Reference> references = new();        
        while (length < parser.Length)
        {
            Parser attempt = new(parser, length);
            var parsed = Reference.Parse(attempt);
            if (parsed is Error error) return error;
            if (parsed is null)
            {
                if (references.Count is 0) return null;
                else break;
            }
            else
            {
                references.Add(parsed as Reference);
            }            
            length += attempt.Cursor;
        }

        return new Aggregate(parser, length) { Parameters = references.ToArray() };
    }
}