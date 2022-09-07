using Ronin.Token;

namespace Ronin.Grammar;

internal abstract class Syntax
{
    internal Syntax Parent { get; set; }
    
    protected internal readonly List<Location> Locations = new();
    protected internal readonly List<Token.Token> tokens = new();

    protected internal Result TryAdd(Whitespace whitespace) => AddToken(whitespace);

    protected internal Result TryAdd(Error error)
    {
        tokens.Add(error);
        return Result.Error;
    }

    protected internal virtual Result TryAdd(Comment comment) => Result.NotApplied;
    protected internal virtual Result TryAdd(Keyword keyword) => Result.NotApplied;
    protected internal virtual Result TryAdd(Literal literal) => Result.NotApplied;
    protected internal virtual Result TryAdd(Name name) => Result.NotApplied;
    protected internal virtual Result TryAdd(Symbol symbol) => Result.NotApplied;

    protected internal Result AddToken(Token.Token token)
    {
        Locations.Add(new()
        {
            Line = token.Line,
            ColumnStart = token.Column,
            ColumnEnd = token.Column + token.Length,
        });
        tokens.Add(token);
        return Result.Applied;
    }

    internal enum Result { Applied, NotApplied, Completed, Descent, Error }

    internal record struct Location(int Line, int ColumnStart, int ColumnEnd);
}

// part of thing.stuff with.other things;
// import literal;
// var hit count => integer;
// var name words => data type name(function call|literal|datatype name|compiled datum, ...);
// var name words = literal;
// var name words is data type name