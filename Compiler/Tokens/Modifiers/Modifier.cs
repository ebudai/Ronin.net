using Ronin.Compiler;

namespace Ronin.Tokens.Modifiers;

internal static class Modifier
{
    internal static bool IsModifier(this Lexer lexer, string keyword) 
        => lexer.Length > keyword.Length
        && lexer.StartsWith(keyword)
        && char.IsWhiteSpace(lexer[keyword.Length]);
}
