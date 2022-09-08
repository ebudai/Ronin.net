using Ronin.Token;

namespace Ronin.Grammar;

internal abstract class Syntax
{
    internal Syntax Parent { get; set; }
    
    protected internal readonly List<Location> Locations = new();
    protected internal readonly List<Token.Token> tokens = new();

    internal Result Add(Token.Token token) => token switch
    {
        Comment comment => Add(comment),
        Error error => Add(error),
        Keyword keyword => Add(keyword),
        Literal literal => Add(literal),
        Name name => Add(name),
        Symbol symbol => Add(symbol),
        Whitespace whitespace => Add(whitespace),
        _ => throw new NotImplementedException()
    };

    private Result Add(Whitespace whitespace)
    {
        Incorporate(whitespace);
        return Result.Applied;
    }

    private Result Add(Comment comment)
    {
        Incorporate(comment);
        return Result.Applied;
    }

    private Result Add(Error error)
    {
        Incorporate(error);
        return Result.Error;
    }

    protected virtual Result Add(Keyword keyword) => Result.NotApplied;
    protected virtual Result Add(Literal literal) => Result.NotApplied;
    protected virtual Result Add(Name name) => Result.NotApplied;
    protected virtual Result Add(Symbol symbol) => Result.NotApplied;

    protected internal void Incorporate(Token.Token token)
    {
        Locations.Add(new()
        {
            Line = token.Line,
            ColumnStart = token.Column,
            ColumnEnd = token.Column + token.Length,
        });
        tokens.Add(token);
    }

    internal enum Result { Applied, NotApplied, Completed, Descended, Error }

    protected internal record struct Location(int Line, int ColumnStart, int ColumnEnd);
}

// <THING> is function call | literal | datatype name | compiled datum
// part of thing.stuff with.other things;
// import literal;
// var hit count => integer;
// constant name words = <THING>;
// reactive name words => data type name = <THING>;
// function name words { ... }
// function (first => money, second => time) name words { ... }
// function name words (first => money, second => time) { ... }
// function name words (first => money, second => time) name words { ... }
// datatype name words { ... }