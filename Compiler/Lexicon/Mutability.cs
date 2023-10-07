using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Mutability : Keyword
{
    public static new Keyword Lex(ref Lexer lexer)
        => Constant.Lex(ref lexer)
        ?? Variable.Lex(ref lexer)
        ?? Let.Lex(ref lexer);
}

internal class Constant : Mutability
{
    internal const string keyword = "constant";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Constant { Memory = lexer.AdvanceBy(keyword.Length) };
    }
}

internal class Variable : Mutability
{
    internal const string keyword = "var";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Variable { Memory = lexer.AdvanceBy(keyword.Length) };
    }
}

internal class Let : Mutability
{
    internal const string keyword = "let";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Let { Memory = lexer.AdvanceBy(keyword.Length) };
    }
}
