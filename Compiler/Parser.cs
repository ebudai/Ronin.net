using Ronin.Grammar;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;

namespace Ronin.Compiler;

internal interface IParsable<T> where T : IParsable<T>
{
    public static abstract T Parse(ref Parser context);
}

internal ref struct Parser
{
    public Parser(Token[] tokens) => this.tokens = tokens;

    public List<Statement> Parse()
    {
        List<Statement> statements = new();
        
        while (IsNotFinished)
        {
            if (Trivia.Parse(ref this) is not null) continue;

            try
            {
                var statement = Statement.Parse(ref this);
                if (Current is not Terminal and not Sentinel) throw new UnexpectedSyntaxError(ref this);
                statements.Add(statement);
            }
            catch (Error error)
            {
                Index = error.Cursor;
                Errors.Add(error);
            }
        }

        return statements;
    }

    //todo fix line 40 - see if we can add errors to the parser instead of returning them - then we can have T.Parse(ref parser) return T instead of Syntax
    internal List<T> ParseRepeating<T>() where T : class, IParsable<T>
    {
        List<T> parsed = new();
        while (IsNotFinished)
        {
            var syntax = T.Parse(ref this);
            if (syntax is null) break;
            parsed.Add(syntax as T);
        }
        return parsed;
    }

    internal int Index;

    internal ref readonly Token Current => ref tokens[Index];

    internal ref readonly Token this[int index] => ref tokens[Index + index];
    internal readonly ReadOnlySpan<Token> this[Range range] => tokens[range];

    internal bool IsNotFinished => Current is not Sentinel;

    internal List<Error> Errors { get; } = new();

    internal void Advance() 
    {
        do ++Index; while (Current is Trivium);
    }

    internal Token[] Commit(ref Parser context)
    {
        var tokens = context[context.Index..Index].ToArray();
        context = this;        
        return tokens;
    }

    private readonly ReadOnlySpan<Token> tokens;
}
