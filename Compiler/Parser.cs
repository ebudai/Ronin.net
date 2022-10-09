using Ronin.Grammar;
using Ronin.Token;

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
            if (Tokens.Span[Cursor] is Symbol symbol && symbol.IsTerminal) break;

            Syntax statement = PartOf.Parse(ref parser)
                ?? Import.Parse(ref parser)
                ?? Grammar.Declaration.Datum.Parse(ref parser)
                ?? Reference.Parse(ref parser);
            if (statement is null) break;
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    internal (string[], int) ParseHierarchy()
    {
        List<string> hierarchy = new() { string.Empty };
        int tokensConsumed = 1;
        for (int max = Length; tokensConsumed != max; ++tokensConsumed)
        {
            var lexeme = this[tokensConsumed];
            if (lexeme is Whitespace) continue;

            if (lexeme is Symbol symbol && symbol.IsTerminal) break;

            string text;
            if (lexeme is Name name) text = name.ToString();
            else if (lexeme is Keyword word) text = word.ToString();
            else return (null, tokensConsumed);

            var names = text.Split(Symbol.hierarchy);
            if (hierarchy[^1].Length is not 0) hierarchy[^1] += ' ';
            hierarchy[^1] += names[0];
            if (names.Length is > 1) hierarchy.AddRange(names[1..]);
        }

        var array = hierarchy.Count is 1 && hierarchy[0].Length is 0 ? null : hierarchy.ToArray();
        return (array, tokensConsumed + 1); // one extra for the terminal
    }

    internal class Exception : System.Exception
    {
        internal Exception(string message) : base(message) { }
    }
}
