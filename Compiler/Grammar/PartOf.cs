using Ronin.Token;

namespace Ronin.Grammar;

internal class PartOf : Syntax
{
    internal List<string> Name { get; } = new();

    private bool _hasKeyword = false;

    protected override Result Add(Keyword keyword)
    {
        if (_hasKeyword)
        {
            AppendToName(keyword);
            Incorporate(keyword);
            return Result.Applied;
        }

        if (keyword.Type is Keyword.Word.part_of)
        {
            _hasKeyword = true;
            Incorporate(keyword);
            return Result.Applied;
        }

        return Result.NotApplied;
    }

    protected override Result Add(Name name)
    {
        if (_hasKeyword)
        {
            AppendToName(name);
            Incorporate(name);
            return Result.Applied;
        }

        return Result.NotApplied;
    }

    protected override Result Add(Symbol symbol)
    {
        if (_hasKeyword && symbol.IsTerminal)
        {
            Incorporate(symbol);
            return Result.Completed;
        }

        return Result.NotApplied;
    }

    private void AppendToName(Keyword keyword) => AppendToName(GetNames(keyword));
    
    private void AppendToName(Name name) => AppendToName(GetNames(name));
    
    private void AppendToName(string[] names)
    {
        if (Name.Count is 0)
        {
            Name.AddRange(names);
        }
        else
        {
            Name[^1] += " " + names[0];
            if (names.Length is > 1) Name.AddRange(names[1..]);
        }
    }

    private static string[] GetNames(Token.Token token) => token.Sourcecode.ToString().Split('/').Select(word => word.Trim()).ToArray();
}
