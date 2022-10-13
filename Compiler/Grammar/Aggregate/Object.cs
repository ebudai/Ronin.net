using Ronin.Compiler;
using Ronin.Token.Delimiter;

namespace Ronin.Grammar.Aggregate;

internal class Object : Syntax, IParsable
{
    internal Reference[] Parameters { get; init; }

    internal Object(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(ref Parser parser)
    {
        if (parser.IsEmpty || parser[0] is not OpenParenthesis) return null;

        Parser attempt = new(parser);
        List<Reference> references = new();
        int length = 0;
        while (parser[length] is not CloseParenthesis)
        {
            var parsed = Reference.Parse(ref attempt);
            if (parsed is Reference reference)
            {
                references.Add(reference);
                length += attempt.Cursor - parser.Cursor;
            }
            else if (parsed is Expected expected)
            {
                return expected;
            }
        }

        return new Object(parser, length) { Parameters = references.ToArray() };
    }
}