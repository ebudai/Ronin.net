// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Lexicon.Literals;

internal class Url : Literal
{
    public static new Url Lex(ref Lexer lexer)
    {
        if (lexer.Length is < 5) return null;

        // get scheme
        int length = 0;
        while (length < lexer.Length && IsValidSchemeCharacter(lexer[length])) ++length;
        if (length == lexer.Length) return null;

        if (length + 4 >= lexer.Length || lexer[length] is not ':' || lexer[length + 1] is not '/' || lexer[length + 2] is not '/') return null;

        length += 3;
        while (length < lexer.Length && IsValidUrlCharacter(lexer[length])) ++length;
        
        // remove trailing terminal if followed by a whitespace or eof
        if (lexer[length - 1] is Terminal.symbol)
        {
            if (length == lexer.Length || char.IsWhiteSpace(lexer[length])) length -= 1;
        }

        return new Url { Memory = lexer.Commit(length) };
    }

    private static bool IsValidSchemeCharacter(char value) => char.IsLetterOrDigit(value) || value is '+' or '-' or '.';

    private static bool IsValidUrlCharacter(char value) 
        => char.IsLetterOrDigit(value) 
        || value is '-' 
                or '.' 
                or '_' 
                or '~' 
                or ':' 
                or '/' 
                or '?' 
                or '#' 
                or '[' 
                or ']' 
                or '@' 
                or '!' 
                or '$' 
                or '&' 
                or '\'' 
                or '(' 
                or ')' 
                or '*' 
                or '+' 
                or ',' 
                or ';' 
                or '%' 
                or '=';
}
