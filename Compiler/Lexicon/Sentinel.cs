namespace Ronin.Lexicon;

internal class Sentinel : Token
{
    public static readonly Sentinel Instance = new();

    private Sentinel() : base(new(string.Empty), 0) { }
}
