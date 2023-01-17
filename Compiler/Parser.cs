using Ronin.Grammar;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Compiler;

internal interface IParsable
{
    public static abstract Syntax Parse(ref Parser parser);
}

internal interface IElement<T> where T : IElement<T>
{
    public static abstract implicit operator T(Syntax syntax);
}

public ref struct Parser
{
    public Parser(Token[] tokens) => this.tokens = tokens;

    public Syntax[] Parse()
    {
        List<Syntax> statements = new();

        while (IsNotFinished)
        {
            var trivia = Trivia.Parse(ref this);
            if (trivia is not null) continue;
            var statement = Statement.Parse(ref this);
            if (statement is Error error) Index = error.Cursor;
            if (Current is not Terminal and not Sentinel) statement = ExpectedTerminalError.Parse(ref this);
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    internal int Index;

    internal ref readonly Token Current => ref tokens[Index];

    internal ref readonly Token this[int index] => ref tokens[Index + index];
    internal ReadOnlySpan<Token> this[Range range] => tokens[range];

    internal bool IsNotFinished => Current is not Sentinel;

    internal void Advance() 
    {
        do ++Index; while (Current is Trivium);
    }

    internal SourceLocation[] Commit(ref Parser context)
    {
        var tokens = context[context.Index..Index];
        List<SourceLocation> sources = new();
        foreach (var token in tokens) sources.Add(token.SourceLocation);
        context.Index = Index;
        return sources.ToArray();
    }

    private readonly ReadOnlySpan<Token> tokens;
}
