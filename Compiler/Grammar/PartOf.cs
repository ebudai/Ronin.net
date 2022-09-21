using Ronin.Token;

namespace Ronin.Grammar;

internal class PartOf : Syntax
{
    internal Identifier Name { get; } = new();

    private bool initialized = false;

    protected override Result Add(Keyword keyword)
    {
        if (initialized) return Name.Add(keyword);

        if (keyword.Type is Keyword.Word.part_of)
        {
            initialized = true;
            Incorporate(keyword);
            return Result.Applied;
        }

        return Result.DoesNotApply;
    }

    protected override Result Add(Name name) => initialized ? Name.Add(name) : Result.DoesNotApply;
}
