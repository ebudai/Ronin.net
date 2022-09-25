using Ronin.Token;

namespace Ronin.Grammar;

internal class PartOf : Syntax
{
    internal Identifier Name { get; } = new();

    private bool initialized = false;

    internal override Result Add(Keyword keyword)
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

    internal override Result Add(Name name) => initialized ? Name.Add(name) : Result.DoesNotApply;

    internal override Result Add(Symbol symbol) => symbol.IsTerminal ? Result.Completed : Result.DoesNotApply;
}
