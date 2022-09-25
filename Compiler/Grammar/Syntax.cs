using Ronin.Compiler;
using Ronin.Token;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

internal abstract class Syntax
{
    internal Syntax Parent { get; set; }
    
    protected internal readonly List<Location> Locations = new();
    protected internal readonly List<Token.Token> Tokens = new();

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

    internal Result Add(Whitespace whitespace)
    {
        Incorporate(whitespace);
        return Result.Applied;
    }

    internal Result Add(Comment comment)
    {
        Incorporate(comment);
        return Result.Applied;
    }

    [DoesNotReturn]
    internal Result Add(Error error)
    {
        Incorporate(error);
        throw new Parser.Exception(error.Sourcecode.ToString());
    }

    internal virtual Result Add(Symbol symbol)
    {
        var result = symbol switch
        {
            { IsOpenBrace: true } => Result.Descended,
            { IsOpenParenthesis: true } => Result.Descended,
            { IsOpenSquareBracket: true } => Result.Descended,
            { IsTerminal: true } => Tokens.Count is > 0 ? Result.Completed : Result.DoesNotApply,
            { IsSeparator: true } => Tokens.Count is > 0 ? Result.Completed : Result.DoesNotApply,
            { IsCloseBrace: true } => Tokens.Any(token => token is Symbol symbol && symbol.IsOpenBrace) ? Result.Completed : Result.DoesNotApply,
            { IsCloseParenthesis: true } => Tokens.Any(token => token is Symbol symbol && symbol.IsOpenParenthesis) ? Result.Completed : Result.DoesNotApply,
            { IsCloseSquareBracket: true } => Tokens.Any(token => token is Symbol symbol && symbol.IsOpenSquareBracket) ? Result.Completed : Result.DoesNotApply,
            _ => Result.DoesNotApply
        };
        if (result is not Result.DoesNotApply and not Result.Descended) Incorporate(symbol);
        return result;
    }

    internal virtual Result Add(Keyword keyword) => Result.DoesNotApply;
    internal virtual Result Add(Literal literal) => Result.DoesNotApply;
    internal virtual Result Add(Name name) => Result.DoesNotApply;    

    protected internal void Incorporate(Token.Token token)
    {
        Locations.Add(new()
        {
            Line = token.Line,
            ColumnStart = token.Column,
            ColumnEnd = token.Column + token.Length,
        });
        Tokens.Add(token);
    }

    internal enum Result { Applied, DoesNotApply, Completed, Descended }

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