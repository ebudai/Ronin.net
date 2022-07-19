namespace Ronin.Transpiler.Grammar.Tokens;

internal class Name : Token
{
    public string Value { get; set; }

    public override string ToString() => Value;
}
