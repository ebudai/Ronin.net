using Ronin.Grammar;
using Ronin.Grammar.Declaration;
using Ronin.Token;
using Ronin.Token.Delimiter;

namespace Ronin.Compiler;

public class Parser
{
    public Parser(Lexeme[] tokens)
    {
        Tokens = tokens;
    }

    internal Parser(Parser parser, int advance)
    {
        Tokens = parser.Tokens;
        Cursor = parser.Cursor + advance;
    }

    internal ReadOnlyMemory<Lexeme> Tokens { get; }
    internal int Cursor { get; set; }
    internal bool IsEmpty => Span.IsEmpty;
    internal int Length => Span.Length;

    internal ReadOnlySpan<Lexeme> Span => Tokens[Cursor..].Span;
    internal Lexeme this[int index] => Span[index];
    internal ReadOnlyMemory<Lexeme> this[Range range] => Tokens[Cursor..][range];

    internal Syntax[] Parse()
    {
        List<Syntax> statements = new();

        var parser = this;
        while (Cursor < Tokens.Length)
        {
            if (IsEmpty) break;
            // break;

            /*var syntax = PartOf.Parse(ref parser);
            if (syntax is not Expected expected)
            {
                statements.Add(syntax);
                continue;
            }*/

            if (TryParse<PartOf>(parser, statements)) continue;
            if (TryParse<Import>(parser, statements)) continue;
            if (TryParse<Datum>(parser, statements)) continue;
            if (TryParse<Reference>(parser, statements)) continue;

            if (Tokens.Span[Cursor] is Terminal) ++Cursor;
        }

        return statements.ToArray();
        
        static bool TryParse<T>(Parser parser, List<Syntax> statements) where T : IParsable
        {
            var syntax = T.Parse(ref parser);
            if (syntax is not null) statements.Add(syntax);
            return syntax is T;
        }
    }

    internal (string[], int) ParseHierarchy()
    {
        List<string> hierarchy = new() { string.Empty };
        int tokensConsumed = 1;
        for (int max = Length; tokensConsumed != max; ++tokensConsumed)
        {
            var lexeme = this[tokensConsumed];
            if (lexeme is Whitespace or Comment) continue;

            if (lexeme is Terminal) break;

            string text;
            if (lexeme is Name name) text = name.ToString();
            else if (lexeme is Keyword word) text = word.ToString();
            else if (lexeme is Hierarchy) text = Hierarchy.character.ToString();
            else return (null, tokensConsumed);

            var names = text.Split(Hierarchy.character);
            if (hierarchy[^1].Length is not 0) hierarchy[^1] += ' ';
            hierarchy[^1] += names[0];
            if (names.Length is > 1) hierarchy.AddRange(names[1..]);
        }

        var array = hierarchy.Count is 1 && hierarchy[0].Length is 0 ? null : hierarchy.ToArray();
        return (array, tokensConsumed + 1); // one extra for the terminal
    }
}
