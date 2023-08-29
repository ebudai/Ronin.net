using Ronin.Grammar;

namespace Ronin.Hierarchy;

internal class Resolution
{
    public Context.Member Member { get; set; }
    public List<Resolution> Resolutions { get; } = new();

    public int Size
    {
        get
        {
            int count = 1;
            foreach (var resolution in Resolutions) count += resolution.Size;
            return count;
        }
    }
}