using Ronin.Token;

namespace Ronin.Grammar;

internal abstract class Syntax
{
    internal Syntax Parent { get; set; }
    
    protected internal readonly List<Location> Locations = new();
    protected internal ReadOnlyMemory<char> Sourcecode;

    internal virtual bool TryAdd(Whitespace whitespace) => false;
    internal virtual bool TryAdd(Name name) => false;
    internal virtual bool TryAdd(Error error) => false;
    internal virtual bool TryAdd(Comment comment) => false;
    internal virtual bool TryAdd(Literal literal) => false;
    /*internal virtual bool TryAdd() => false;
    internal virtual bool TryAdd() => false;
    internal virtual bool TryAdd() => false;
    internal virtual bool TryAdd() => false;
    internal virtual bool TryAdd() => false;
    internal virtual bool TryAdd() => false;
    internal virtual bool TryAdd() => false;
    internal virtual bool TryAdd() => false;
    internal virtual bool TryAdd() => false;
    internal virtual bool TryAdd() => false;*/

    internal readonly struct Location
    {
        internal readonly int Line;
        internal readonly int ColumnStart;
        internal readonly int ColumnEnd;
    }
}
