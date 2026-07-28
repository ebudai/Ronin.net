// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Keyword : Word
{
    public static new Word Lex(ref Lexer lexer)
        => Modifier.Lex(ref lexer)
        ?? Mutability.Lex(ref lexer)
        ?? Type.Lex(ref lexer)
        ?? Extend.Lex(ref lexer)
        ?? ForEach.Lex(ref lexer)
        ?? In.Lex(ref lexer)
        ?? Function.Lex(ref lexer)
        ?? Import.Lex(ref lexer)
        ?? PartOf.Lex(ref lexer)
        ?? If.Lex(ref lexer)
        ?? While.Lex(ref lexer) 
        ?? Changing.Lex(ref lexer)
        ?? When.Lex(ref lexer) as Keyword;

    protected static T Lex<T>(ref Lexer lexer, string keyword) where T : Keyword, new()
    {
        if (lexer.StartsWith(keyword) is false) return null;

        // A keyword needs a boundary after it, or «iffy» would lex as «if».
        // Reaching the end of the source IS a boundary — reading one past it to
        // check was an IndexOutOfRangeException for a file ending in «if».
        if (lexer.Length > keyword.Length && char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;

        return new T { Memory = lexer.AdvanceBy(keyword.Length) };
    }
}

internal class Type : Keyword
{
    internal const string keyword = "type";

    public static new Type Lex(ref Lexer lexer) => Lex<Type>(ref lexer, keyword);
}

internal class Extend : Keyword
{
    internal const string keyword = "extend";

    public static new Extend Lex(ref Lexer lexer) => Lex<Extend>(ref lexer, keyword);
}

/// <summary>
///     Opens a loop: «for each bank in banks».
/// </summary>
///
/// <remarks>
///     One token spelling two words, as <see cref="PartOf"/> already is, so that
///     «for» on its own stays an ordinary word — «compute total for order» is a
///     pattern the language wants and reserving «for» would take it away.
/// </remarks>
internal class ForEach : Keyword
{
    internal const string keyword = "for each";

    public static new ForEach Lex(ref Lexer lexer) => Lex<ForEach>(ref lexer, keyword);
}

/// <summary>
///     Separates a loop's variable from what it walks.
/// </summary>
///
/// <remarks>
///     <para>
///     Reserved outright, which is the stronger of the two options the design
///     note left open. R5 alone would reserve «in» only inside multi-word names
///     and leave «var in» legal; a keyword reserves it everywhere, and makes the
///     rule one sentence rather than a rule with an exception.
///     </para>
///     <para>
///     It also makes the loop header split structurally rather than by scoring.
///     «for each bank in banks» has exactly one «in» and the parser knows which,
///     without needing the symbol table — and R5 keeps a second one from ever
///     appearing, which is what the design note proves is load-bearing.
///     </para>
/// </remarks>
internal class In : Keyword
{
    internal const string keyword = "in";

    public static new In Lex(ref Lexer lexer) => Lex<In>(ref lexer, keyword);
}

internal class Function : Keyword
{
    internal const string keyword = "function";

    public static new Function Lex(ref Lexer lexer) => Lex<Function>(ref lexer, keyword);
}

internal class Import : Keyword
{
    internal const string keyword = "import";

    public static new Import Lex(ref Lexer lexer) => Lex<Import>(ref lexer, keyword);
}

internal class PartOf : Keyword
{
    internal const string keyword = "part of";

    public static new PartOf Lex(ref Lexer lexer) => Lex<PartOf>(ref lexer, keyword);
}

internal class If : Keyword
{
    internal const string keyword = "if";

    public static new If Lex(ref Lexer lexer) => Lex<If>(ref lexer, keyword);
}

internal class When : Keyword
{
    internal const string keyword = "when";

    public static new When Lex(ref Lexer lexer) => Lex<When>(ref lexer, keyword);
}

internal class While : Keyword
{
    internal const string keyword = "while";

    public static new While Lex(ref Lexer lexer) => Lex<While>(ref lexer, keyword);
}

internal class Changing : Keyword
{
    internal const string keyword = "changing";

    public static new Changing Lex(ref Lexer lexer) => Lex<Changing>(ref lexer, keyword);
}
