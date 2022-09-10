using Ronin.Token;

namespace Ronin.Grammar;

internal class PartOf : Syntax
{
    internal Identifier Name { get; set; } = new();

    private bool initialized = false;

    protected override Result Add(Keyword keyword)
    {
        if (initialized)
        {
            Name += keyword;
            Incorporate(keyword);
            return Result.Applied;
        }

        if (keyword.Type is Keyword.Word.part_of)
        {
            initialized = true;
            Incorporate(keyword);
            return Result.Applied;
        }

        return Result.DoesNotApply;
    }

    protected override Result Add(Name name)
    {
        if (initialized)
        {
            Name += name;
            Incorporate(name);
            return Result.Applied;
        }

        return Result.DoesNotApply;
    }
}
