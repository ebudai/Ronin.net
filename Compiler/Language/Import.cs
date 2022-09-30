using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Token;

namespace Ronin.Language;

/*internal class Import : Syntax
{
    internal Identifier Name { get; } = new();
    internal Literal Url { get; set; }

    private bool initialized = false;

    internal override Result Add(Keyword keyword)
    {
        if (initialized) return Name.Add(keyword);

        if (keyword.Type is Keyword.Word.import)
        {
            initialized = true;
            Incorporate(keyword);
            return Result.Applied;
        }

        return Result.DoesNotApply;
    }

    internal override Result Add(Name name) => initialized ? Name.Add(name) : Result.DoesNotApply;

    internal override Result Add(Literal literal)
    {
        if (!initialized) return Result.DoesNotApply;

        if (Url is not null) throw new Parser.Exception("already specified import url");

        Incorporate(literal);
        Url = literal;
        return Result.Applied;
    }

    internal override Result Add(Symbol symbol) => symbol.IsTerminal ? Result.Completed : Result.DoesNotApply;
}
*/