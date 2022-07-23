namespace Ronin.Transpiler;

internal abstract class Statement 
{
    internal readonly Token start;

    internal Statement(Token start) => this.start = start;
}
