using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Import : Syntax
{
    internal Identifier Name { get; set; } = new();
    internal Literal Url { get; set; }

    private bool initialized = false;

    protected override Result Add(Keyword keyword)
    {
        if (initialized)
        {
            Name += keyword;
            Incorporate(keyword);
            return Result.Applied;
        }

        if (keyword.Type is Keyword.Word.import)
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

    protected override Result Add(Literal literal)
    {
        if (!initialized) return Result.DoesNotApply;

        if (Url is not null) throw new Parser.Exception("already specified import url");

        Incorporate(literal);
        Url = literal;
        return Result.Applied;
    }
}
