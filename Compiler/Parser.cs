using Ronin.Grammar;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;

namespace Ronin.Compiler;

public interface IParsable
{
    public static abstract Syntax Parse(ref Parser context);
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
            else if (Current is not Terminal and not Sentinel) statement = UnexpectedSyntaxError.Parse(ref this);
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    //todo fix line 40 - see if we can add errors to the parser instead of returning them - then we can have T.Parse(ref parser) return T instead of Syntax
    public List<T> Parse<T>() where T : class, IParsable
    {
        List<T> parsed = new();
        while (IsNotFinished)
        {
            var syntax = T.Parse(ref this);
            //if (syntax is Error) return syntax;
            if (syntax is null) break;
            parsed.Add(syntax as T);
        }
        return parsed;
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

    internal Token[] Commit(ref Parser context)
    {
        var tokens = context[context.Index..Index].ToArray();
        context = this;        
        return tokens;
    }

    private readonly ReadOnlySpan<Token> tokens;
}
