using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal interface IParsable
{
    public static abstract Syntax Parse(Parser parser);
}

internal abstract class Syntax
{
    protected internal ReadOnlyMemory<Token> Tokens { get; init; }
}

/*
[+ means and/or]

declare function - declarator then identifier then scope
declare datatype - modifiers then declarator then identifier then algebra then scope
declare datum - declarator then parameter
identifier - name + parameters ...
reference - name + value ...
x import - 'import' then name
x partof - 'part of' then name

x declarator - 'var' or 'constant' or 'let' or 'function' or 'datatype'
x modifiers - 'optional' or 'compiled' or 'persistent' or 'shared'
x name - word + wordable symbol ... [symbols don't need to be separated]
x parameter - explicit - name then => then modifiers then reference [datatype] then = then value (optionally) [initializer]
          - implicit - name then = then value
x parameters - '(' then parameter, ... then ')'
scope - '{' then statement; ... then '}'
algebra - modifiers then reference then 'and' or 'or' ...
x scalar - literal ...
x value - scalar or aggregate or reference or declaration [ie: returns a function tearaway or datatype value etc]
statement - value then ';'
aggregate - '(' then value, ... then ')'
index - '[' then value,... then ']'

*/