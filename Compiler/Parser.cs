using Ronin.Grammar;
using Ronin.Grammar.Declaration;
using Ronin.Token;
using Ronin.Token.Delimiter;
using System.Text.RegularExpressions;

namespace Ronin.Compiler;

public class Parser
{
    public Parser(Lexeme[] tokens)
    {
        Tokens = tokens;
    }

    internal Parser(Parser parser, int advance = 0)
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

            if (TryParse<PartOf>(parser, statements)) continue;
            if (TryParse<Import>(parser, statements)) continue;
            if (TryParse<Datum>(parser, statements)) continue;
            if (TryParse<Trivium>(parser, statements)) continue;
            if (TryParse<Reference>(parser, statements)) continue;
            
            //todo handle case where everything above generated only Expecteds

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
        string hierarchy = string.Empty;
        int tokensConsumed = 2;
        for (int max = Length; tokensConsumed != max; ++tokensConsumed)
        {
            var lexeme = this[tokensConsumed];
            if (lexeme is Comment) continue;

            if (lexeme is Terminal) break;

            string text = lexeme switch
            {
                Name name => name.ToString(),
                Keyword keyword => keyword.ToString(),
                Hierarchy => Hierarchy.character.ToString(),
                Whitespace => " ",
                _ => null
            };

            if (text is null) return (null, tokensConsumed);

            hierarchy += text;
        }

        var levels = hierarchy.Split(Hierarchy.character, StringSplitOptions.RemoveEmptyEntries);
        if (levels.Length is 0) levels = null;
        return (levels, tokensConsumed + Terminal.character.ToString().Length); // one extra for the terminal
    }
}
