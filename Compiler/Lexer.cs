namespace Ronin.Compiler;

internal class Lexer
{
    internal ReadOnlyMemory<char> Sourcecode { get; set; }
    internal int Cursor { get; set; }
    internal string Error { get; set; }
    internal List<(int index, string warning)> Warnings { get; } = new();
}
