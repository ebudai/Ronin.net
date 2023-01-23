// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Grammar;

internal abstract class Syntax
{
    protected internal Token[] Source { get; init; }
}
/*

[+ means and/or]

x declare function - modifiers 'function' identifier scope
x declare datatype - modifiers 'datatype' identifier { '=' reference } (optional) [algebra] scope
x declare datum - declarator parameter
x identifier - name + parameters ...
x reference - name + arguments ...
x import - 'import' name
x partof - 'part of' name

x declarator - 'var' or 'constant' or 'let'
x modifiers - 'optional' or 'compiled' or 'persistent' or 'shared'
x name - word + wordable symbol ... [symbols don't need to be separated]
x assignment - name '=' value
x parameter - explicit - name => modifiers reference [datatype] { '=' value } (optionally) [initializer]
            - implicit - assignment
x scalar - literal ...
x value - scalar or reference or scope

x error - all until ';'

aggregates
x arguments - '(' value, ... ')'
x index - '[' value,... ']'
x scope - '{' value; ... '}'
x parameters - '(' parameter + assignment, ... ')'

*/