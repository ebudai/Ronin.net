namespace Ronin.Transpiler;

internal abstract class Statement 
{
    internal readonly Token start;

    internal Statement(Token start) => this.start = start;

    internal static void Expect(bool condition, string message)
    {
        if (!condition) throw new Parser.Exception(message);
    }
}
