using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Modifier : Keyword
{
    public static new Keyword Lex(ref Lexer lexer)
        => Compiled.Lex(ref lexer)
        ?? Hidden.Lex(ref lexer)
        ?? Reactive.Lex(ref lexer)
        ?? Fast.Lex(ref lexer)
        ?? Global.Lex(ref lexer);
}

/// <summary>
///     «fast number» — one number type with a representation hint, for «/fp:fast»
///     on a single variable.
/// </summary>
///
/// <remarks>
///     A MODIFIER and not a seventh type name, ruled in TYPEHALFRULINGS §1: a name
///     in the type kind IS a type, so a second spelling would put a second number
///     type in the table whether anyone wanted one or not. The modifier keeps one
///     number type and hangs the hint off it, so nothing downstream that unifies
///     numbers ever sees two. It is a keyword like every modifier — the lexer
///     produces it everywhere — so no name may contain the word and it cannot be
///     captured silently, which is FIVE-RULINGS §0 asked of the table it lives in.
/// </remarks>
internal class Fast : Modifier
{
    internal const string keyword = "fast";

    public static new Keyword Lex(ref Lexer lexer)
    {
        return Lex<Fast>(ref lexer, keyword);
    }
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

internal class Reactive : Modifier
{
    internal const string keyword = "reactive";

    public static new Keyword Lex(ref Lexer lexer)
    {
        return Lex<Reactive>(ref lexer, keyword);
    }
}
