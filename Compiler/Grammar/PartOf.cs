using Ronin.Token;

namespace Ronin.Grammar;

internal class PartOf : Syntax
{
    internal List<string> Name { get; } = new();

    private bool _hasKeyword = false;

    internal override Result Add(Keyword keyword)
    {
        if (!_hasKeyword)
        {
            if (keyword.Type is Keyword.Word.part_of)
            {
                _hasKeyword = true;
                return Result.Applied;
            }

            return Result.NotApplied;
        }

        return Incorporate(keyword);
    }

    internal override Result Add(Name name) => _hasKeyword ? Incorporate(name) : Result.NotApplied;

    internal override Result Add(Symbol symbol) => symbol.IsTerminal ? Incorporate(symbol, Result.Completed) : Result.Error;

    protected internal override Result Incorporate(Token.Token token, Result result = Result.Applied)
    {
        if (token is Name || token is Keyword)
        {
            var names = GetNames(token);
            if (Name.Count is 0)
            {
                Name.AddRange(names);
            }
            else
            {
                this.Name[^1] += " " + names[0];
                if (names.Length is > 1) Name.Add(names[1]);
            }
        }
        return base.Incorporate(token, result);
    }

    //TODO I think .NET7 adds Split() to ReadOnlySpan<T>
    private static string[] GetNames(Token.Token token) => token.Sourcecode.ToString().Split('/').Select(word => word.Trim()).ToArray();


}
