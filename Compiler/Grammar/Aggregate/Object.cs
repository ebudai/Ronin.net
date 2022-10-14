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

        int length = OpenParenthesis.character.ToString().Length;
        Parser attempt = new(parser, length);
        List<Reference> references = new();        
        while (parser[length] is not CloseParenthesis)
        {
            var parsed = Reference.Parse(ref attempt);
            if (parsed is Reference reference)
            {
                references.Add(reference);
                length += parsed.Tokens.Count;
            }
            else
            {
                return parsed as Expected;
            }
        }

        return new Object(parser, length + CloseParenthesis.character.ToString().Length) { Parameters = references.ToArray() };
    }
}