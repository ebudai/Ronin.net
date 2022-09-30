using Ronin.Grammar;
using Ronin.Token;

namespace Ronin.Language;

/*internal class Identifier : Syntax
{
    internal Identifier() => InitializeName();

    internal SortedList<int, string> NameParts { get; } = new();
    private List<Function> Parameters { get; } = new();

    public static implicit operator string(Identifier @this) => @this._name.Value;
    
    private Lazy<string> _name;

    internal string[] Hierarchy => string.Join(' ', NameParts.Values).Split('/'); //TODO move to PartOf

    internal override Result Add(Keyword keyword)
    {
        NameParts.Add(NameParts.Count + Parameters.Count, keyword.Sourcecode.ToString());
        Incorporate(keyword);
        InitializeName();
        return Result.Applied;
    }

    internal override Result Add(Name name)
    {
        NameParts.Add(NameParts.Count + Parameters.Count, name.Sourcecode.ToString());
        Incorporate(name);
        InitializeName();
        return Result.Applied;
    }

    internal override Result Add(Symbol symbol)
    {
        if (symbol.IsReturns)
        {
            NameParts.Add(NameParts.Count + Parameters.Count, symbol.Sourcecode.ToString());
            Incorporate(symbol);
            InitializeName();
            return Result.Applied;
        }

        return base.Add(symbol);
    }

    private void InitializeName()
    {
        _name = new(() =>
        {
            var name = string.Empty;
            for (int i = 0, max = NameParts.Count + Parameters.Count; i != max; ++i)
            {
                if (i is not 0) name += ' ';
                name += NameParts.ContainsKey(i) ? NameParts[i] : '?';
            }
            return name;
        }, isThreadSafe: true);
    }
}*/
