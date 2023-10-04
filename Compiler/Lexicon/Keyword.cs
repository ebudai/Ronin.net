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
        ?? Iterate.Lex(ref lexer)
        ?? Function.Lex(ref lexer)
        ?? Import.Lex(ref lexer)
        ?? PartOf.Lex(ref lexer)
        ?? If.Lex(ref lexer)
        ?? While.Lex(ref lexer);

    protected static T Lex<T>(ref Lexer lexer, string keyword) where T : Keyword, new()
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new T { Memory = lexer.Commit(keyword.Length) };
    }
}

internal class Type : Keyword
{
    internal const string keyword = "type";

    public static new Keyword Lex(ref Lexer lexer) => Lex<Type>(ref lexer, keyword);
}

internal class Extend : Keyword
{
    internal const string keyword = "extend";

    public static new Keyword Lex(ref Lexer lexer) => Lex<Extend>(ref lexer, keyword);
}

internal class Iterate : Keyword
{
    internal const string keyword = "iterate";

    public static new Keyword Lex(ref Lexer lexer) => Lex<Iterate>(ref lexer, keyword);
}

internal class Function : Keyword
{
    internal const string keyword = "function";

    public static new Keyword Lex(ref Lexer lexer) => Lex<Function>(ref lexer, keyword);
}

internal class Import : Keyword
{
    internal const string keyword = "import";

    public static new Keyword Lex(ref Lexer lexer) => Lex<Import>(ref lexer, keyword);
}

internal class PartOf : Keyword
{
    internal const string keyword = "part of";

    public static new Keyword Lex(ref Lexer lexer) => Lex<PartOf>(ref lexer, keyword);
}

internal class If : Keyword
{
    internal const string keyword = "if";

    public static new Keyword Lex(ref Lexer lexer) => Lex<If>(ref lexer, keyword);
}

internal class When : Keyword
{
    internal const string keyword = "when";

    public static new Keyword Lex(ref Lexer lexer) => Lex<When>(ref lexer, keyword);
}

internal class While : Keyword
{
    internal const string keyword = "while";

    public static new Keyword Lex(ref Lexer lexer) => Lex<While>(ref lexer, keyword);
}