using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal interface IParsable
{
    public static abstract Syntax Parse(Parser parser);
}

internal abstract class Syntax
{
    protected internal Syntax(Parser parser, int length)
    {
        Tokens.AddRange(parser[..length].ToArray());
        parser.Cursor += length;
    }

    //internal Syntax Parent { get; set; }

    protected internal readonly List<Lexicon.Token> Tokens = new();

    /*protected internal record struct Location(int Line, int ColumnStart, int ColumnEnd)
    {
        internal Location(Lexeme token) : this(token.Line, token.Column, token.Column + token.Length) { }
    }*/
}

/*
[+ means and/or]

declare function - 'function' then name + parameters ... then scope
declare datatype - modifiers then 'datatype' then name + parameters ... then algebra then scope
declare datum - modifiers, 'var' (optionally) or 'constant', name, => datatype reference (optionally), = initializer (optionally) [must have one or both of datatype and initializer]
function reference - '(' (optionally) then name + arguments ... then ')' (optionally) [parens must be both or neither]
datatype reference - name + arguments ...
datum reference - name
value - literal ... [ie: for datetimes]
special - 'import' or 'partof' then name

modifiers - datum - compiled or persistent or reactive or shared [only one of each allowed, multiples mean its part of a name]
          - datatype - optional
name - word + wordable symbol ... [symbols don't need to be separated]
parameters - '(' then declare datum, ... then ')'
scope - '{' then statement; ... then '}'
algebra - datatype reference then 'and' or 'or' ...
initializer - value or datum reference
arguments - '(' then value or datum reference or function reference, ... then ')'
statement - declaration or reference then ';'

*/