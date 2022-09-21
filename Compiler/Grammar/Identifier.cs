using Ronin.Token;

namespace Ronin.Grammar;

internal class Identifier : Syntax
{
    internal SortedList<int, string> Name { get; } = new();
    private List<Function> Parameters { get; } = new();

    internal string[] Hierarchy => string.Join(' ', Name.Values).Split('/'); //TODO move to PartOf

    protected override Result Add(Keyword keyword)
    {
        Name.Add(Name.Count + Parameters.Count, keyword.Sourcecode.ToString());
        Incorporate(keyword);
        return Result.Applied;
    }

    protected override Result Add(Name name)
    {
        Name.Add(Name.Count + Parameters.Count, name.Sourcecode.ToString());
        Incorporate(name);
        return Result.Applied;
    }
}
