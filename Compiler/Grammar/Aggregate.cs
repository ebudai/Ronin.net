namespace Ronin.Grammar;

internal class Aggregate<T> : Syntax where T : Syntax
{
    internal List<T> Children { get; } = new();
}
