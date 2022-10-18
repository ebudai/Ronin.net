using Ronin.Grammar;
using Ronin.Grammar.Declaration;
using Ronin.Token;
using Ronin.Token.Symbols;

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
        List<Syntax> parsed = new();

        var parser = this;
        while (Cursor < Tokens.Length)
        {
            if (IsEmpty) break;

            statements.AddRange(parsed);
            parsed.Clear();

            if (TryParse<PartOf>(parser, parsed)) continue;
            if (TryParse<Import>(parser, parsed)) continue;
            if (TryParse<Datum>(parser, parsed)) continue;
            if (TryParse<Trivium>(parser, parsed)) continue;

            if (parsed.All(statement => statement is Expected))
            {
                if (TryParse<Reference>(parser, statements)) continue;
                return parsed.ToArray();
            }

            if (Tokens.Span[Cursor] is Terminal) ++Cursor;
        }

        statements.AddRange(parsed);

        return statements.ToArray();
        
        static bool TryParse<T>(Parser parser, List<Syntax> statements) where T : IParsable
        {
            var syntax = T.Parse(parser);
            if (syntax is not null) statements.Add(syntax);
            return syntax is T;
        }
    }
}
