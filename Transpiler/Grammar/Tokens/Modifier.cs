namespace Ronin.Transpiler.Grammar.Tokens;

internal abstract class Modifier : Token
{
    public abstract Declaration Modifies { get; }
}
