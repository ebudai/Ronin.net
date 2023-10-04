using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Modifier : Keyword
{
    public static new Keyword Lex(ref Lexer lexer)
        => Compiled.Lex(ref lexer)
        ?? Hidden.Lex(ref lexer)
        ?? Optional.Lex(ref lexer)
        ?? Reactive.Lex(ref lexer)
        ?? Global.Lex(ref lexer);
}

internal class Compiled : Modifier
{
    internal const string keyword = "compiled";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Compiled { Memory = lexer.Commit(keyword.Length) };
    }
}

internal class Global : Modifier
{
    internal const string keyword = "global";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Global { Memory = lexer.Commit(keyword.Length) };
    }
}

internal class Hidden : Modifier
{
    internal const string keyword = "hidden";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Hidden { Memory = lexer.Commit(keyword.Length) };
    }
}


internal class Optional : Modifier
{
    internal const string keyword = "optional";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Optional { Memory = lexer.Commit(keyword.Length) };
    }
}

internal class Reactive : Modifier
{
    internal const string keyword = "reactive";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Reactive { Memory = lexer.Commit(keyword.Length) };
    }
}