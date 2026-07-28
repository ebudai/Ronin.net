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
        return Lex<Constant>(ref lexer, keyword);
    }
}

internal class Variable : Mutability
{
    internal const string keyword = "var";

    public static new Keyword Lex(ref Lexer lexer)
    {
        return Lex<Variable>(ref lexer, keyword);
    }
}

internal class Let : Mutability
{
    internal const string keyword = "let";

    public static new Keyword Lex(ref Lexer lexer)
    {
        return Lex<Let>(ref lexer, keyword);
    }
}
