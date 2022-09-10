using Ronin.Token;

namespace Ronin.Grammar;

internal class Identifier
{
    internal List<Component> Components { get; } = new();

    internal List<string> Hierarchy
    {
        get
        {
            if (_hierarchy is not null) return _hierarchy;

            _hierarchy = new();

            foreach (var component in Components)
            {
                var name = (component as Text).Name;
                var names = name.Split('/');
                if (_hierarchy.Count is > 0)
                {
                    _hierarchy[^1] += " " + names[0];
                    if (names.Length is > 1) _hierarchy.AddRange(names[1..]);
                }
                else
                {
                    _hierarchy.AddRange(names);
                }
            }

            return _hierarchy;
        }
    }
    private List<string> _hierarchy = null;

    public static Identifier operator +(Identifier identifier, Keyword keyword)
    {
        identifier.Append(keyword);
        identifier._hierarchy = null;
        return identifier;
    }

    public static Identifier operator +(Identifier identifier, Name name)
    {
        identifier.Append(name);
        identifier._hierarchy = null;
        return identifier;
    }

    private void Append(Token.Token token)
    {
        var value = token.Sourcecode.ToString();
        if (Components.Count is > 0 && Components[^1] is Text text)
        {
            Components[^1] = text with { Name = text.Name + " " + value };
        }
        else
        {
            Components.Add(new Text(value));
        }
    }

    internal record Component 
    {
        public static implicit operator string(Component component) => (component as Text).Name;
        public static implicit operator Datum(Component component) => (component as Parameter).Datum;
    }

    internal record Text(string Name) : Component;

    internal record Parameter(Datum Datum) : Component;
}
