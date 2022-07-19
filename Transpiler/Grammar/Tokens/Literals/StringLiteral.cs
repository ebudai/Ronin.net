namespace Ronin.Transpiler.Grammar.Tokens.Literals;

internal class StringLiteral : Literal
{
    public override string ToString() => @$"""{Value}""";
}
