using Ronin.Compiler;

namespace Ronin.Token;

internal class Keyword : Lexeme
{
    internal const string var = nameof(var);
    internal const string constant = nameof(constant);
    internal const string datatype = nameof(datatype);
    internal const string function = nameof(function);    
    internal const string reactive = nameof(reactive);
    internal const string compiled = nameof(compiled);
    internal const string persistent = nameof(persistent);
    internal const string shared = nameof(shared);
    internal const string optional = nameof(optional);
    internal const string part_of = "part of";
    internal const string import = nameof(import);
    internal const string @return = nameof(@return);

    internal Keyword(Lexer lexer, int length) : base(lexer, length) { }

    internal static Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        
        foreach (var keyword in keywords)
        {
            if (lexer.StartsWith(keyword))
            {
                if (char.IsWhiteSpace(lexer[keyword.Length]) || Symbol.IsSymbol(lexer, keyword.Length))
                {
                    return new Keyword(lexer, keyword.Length);
                }
            }
        }

        return null;
    }

    private static readonly string[] keywords =
    {
        var,
        constant,
        datatype,
        function,
        reactive,
        compiled,
        persistent,
        shared,
        optional,
        part_of,
        import,
        @return
    };
}