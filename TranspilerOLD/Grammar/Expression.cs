namespace Ronin.Transpiler.Grammar;

internal abstract class Expression
{
    public abstract Token[] Signature { get; }
}
