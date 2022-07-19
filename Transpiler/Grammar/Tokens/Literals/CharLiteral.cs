namespace Ronin.Transpiler.Grammar.Tokens.Literals;

internal class CharLiteral : Literal
{
    public override string ToString() => $"'{Value}'";
}
