using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Assignment : Punctuation
{
    public static new Assignment Lex(ref Lexer lexer)
        => Assign.Lex(ref lexer)
        ?? AddAssign.Lex(ref lexer) 
        ?? AndAssign.Lex(ref lexer) 
        ?? DivideAssign.Lex(ref lexer)
        ?? MultiplyAssign.Lex(ref lexer) 
        ?? OrAssign.Lex(ref lexer) 
        ?? SubtractAssign.Lex(ref lexer) as Assignment;
}

internal class Assign : Assignment
{
    internal const char symbol = '=';

    public static new Assign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not symbol) return null;
        return new() { Memory = lexer.Commit(1) };
    }
}

internal class AddAssign : Assignment
{
    internal const string symbol = "+=";

    public static new AddAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new AddAssign { Memory = lexer.Commit(symbol.Length) };
    }
}

internal class AndAssign : Assignment
{
    internal const string symbol = "&=";

    public static new AndAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new AndAssign { Memory = lexer.Commit(symbol.Length) };
    }
}

internal class DivideAssign : Assignment
{
    internal const string symbol = "/=";

    public static new DivideAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new DivideAssign { Memory = lexer.Commit(symbol.Length) };
    }
}

internal class MultiplyAssign : Assignment
{
    internal const string symbol = "*=";

    public static new MultiplyAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new MultiplyAssign { Memory = lexer.Commit(symbol.Length) };
    }
}

internal class OrAssign : Assignment
{
    internal const string symbol = "|=";

    public static new OrAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new OrAssign { Memory = lexer.Commit(symbol.Length) };
    }
}

internal class SubtractAssign : Assignment
{
    internal const string symbol = "-=";

    public static new SubtractAssign Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new SubtractAssign { Memory = lexer.Commit(symbol.Length) };
    }
}
