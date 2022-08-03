namespace Ronin.Parser;

internal abstract class Symbol : Syntax
{
    internal abstract string Value { get; }

    internal static Symbol Get(string symbol) => symbol switch
    {
        "." => new Terminal(),
        "," => new Separator(),
        ")" => new ClosingParenthesis(),
        "]" => new ClosingSquareBracket(),
        "}" => new ClosingBrace(),
        _ => throw new Parser.Exception($"unknown symbol {symbol}")
    };

}

internal class Terminal : Symbol
{
    internal override string Value => ".";
}

internal class Separator : Symbol
{
    internal override string Value => ",";
}

internal class ClosingParenthesis : Symbol
{
    internal override string Value => ")";
}

internal class ClosingSquareBracket : Symbol
{
    internal override string Value => "]";
}

internal class ClosingBrace : Symbol
{
    internal override string Value => "}";
}