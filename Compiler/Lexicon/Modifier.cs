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
        return Lex<Compiled>(ref lexer, keyword);
    }
}

internal class Global : Modifier
{
    internal const string keyword = "global";

    public static new Keyword Lex(ref Lexer lexer)
    {
        return Lex<Global>(ref lexer, keyword);
    }
}

internal class Hidden : Modifier
{
    internal const string keyword = "hidden";

    public static new Keyword Lex(ref Lexer lexer)
    {
        return Lex<Hidden>(ref lexer, keyword);
    }
}


internal class Optional : Modifier
{
    internal const string keyword = "optional";

    public static new Keyword Lex(ref Lexer lexer)
    {
        return Lex<Optional>(ref lexer, keyword);
    }
}

internal class Reactive : Modifier
{
    internal const string keyword = "reactive";

    public static new Keyword Lex(ref Lexer lexer)
    {
        return Lex<Reactive>(ref lexer, keyword);
    }
}
