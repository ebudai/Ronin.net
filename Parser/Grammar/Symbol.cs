namespace Ronin.Parser.Grammar;

internal abstract class Symbol : Syntax
{
    internal abstract string Value { get; }

    internal new static Symbol Parse(Context context) => context.Lex(symbol) switch
    {
        terminal => new Terminal(),
        separator => new Separator(),
        groupingopen => new OpeningParenthesis(),
        listopen => new OpeningSquareBracket(),
        scopeopen => new OpeningBrace(),
        groupingclose => new ClosingParenthesis(),
        listclose => new ClosingSquareBracket(),
        scopeclose => new ClosingBrace(),
        _ => null
    };
}

internal class Terminal : Symbol
{
    internal override string Value => terminal;
}

internal class Separator : Symbol
{
    internal override string Value => separator;
}

internal class OpeningParenthesis : Symbol
{
    internal override string Value => groupingopen;
}

internal class OpeningSquareBracket : Symbol
{
    internal override string Value => listopen;
}

internal class OpeningBrace : Symbol
{
    internal override string Value => scopeopen;
}

internal class ClosingParenthesis : Symbol
{
    internal override string Value => groupingclose;
}

internal class ClosingSquareBracket : Symbol
{
    internal override string Value => listclose;
}

internal class ClosingBrace : Symbol
{
    internal override string Value => scopeclose;
}