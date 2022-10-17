using Ronin.Compiler;
using Ronin.Token.Delimiter;

namespace Ronin.Grammar.Aggregate;

internal class Object : Syntax, IParsable
{
    internal Reference[] Parameters { get; init; }

    internal Object(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        if (parser.IsEmpty || parser[0] is not OpenParenthesis) return new Expected<OpenParenthesis>(parser);

        int length = OpenParenthesis.character.ToString().Length;        
        List<Reference> references = new();        
        while (length < parser.Length && parser[length] is not CloseParenthesis)
        {
            Parser attempt = new(parser, length);
            var parsed = Reference.Parse(attempt);
            if (parsed is not Reference reference) return parsed as Expected;            
            if (attempt.IsEmpty || attempt[0] is not CloseParenthesis and not Separator) return new Expected<Separator, CloseParenthesis>(attempt);
            references.Add(reference);
            length += parsed.Tokens.Count + (attempt[0] is CloseParenthesis ? 1 : 0);
        }

        return new Object(parser, length) { Parameters = references.ToArray() };
    }
}