namespace Ronin.Grammar;

internal abstract class Syntax
{
    internal Syntax Parent { get; set; }
    
    protected internal readonly List<Location> Locations = new();
    protected internal ReadOnlyMemory<char> Sourcecode;

    internal readonly struct Location
    {
        internal readonly ref string Sourcecode;
        internal readonly int Line;
        internal readonly int ColumnStart;
        internal readonly int ColumnEnd;
    }
}
