using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ronin.Parser;

[DebuggerDisplay("{ToString()}")]
internal class Identifier : Syntax
{
    internal Identifier(string name) => names.Add(formatter.Replace(name,  " "));

    internal Identifier(Expression value) => Add(value);

    public override string ToString()
    {
        var name = string.Empty;
        if (parameters.Count is 0) return string.Join(' ', names);
        var max = Math.Max(names.Count, parameters.Keys.Max());

        for (int i = 0; i <= max; ++i)
        {
            if (parameters.ContainsKey(i)) name += "() ";
            if (i < names.Count) name += names[i] + " ";
        }
        return name.TrimEnd();
    }

    internal void Add(Identifier name) => names.AddRange(name.names);
    internal void Add(Expression value) => parameters.Add(names.Count, value);
    internal void Add(Syntax syntax)
    {
        if (!parameters.TryGetValue(names.Count, out var expression))
        {
            expression = new();
            parameters.Add(names.Count, expression);
        }
        expression.Syntax.Add(syntax);
    }

    private readonly List<string> names = new();
    private readonly Dictionary<int, Expression> parameters = new();

    private static readonly Regex formatter = new(@"\s+", RegexOptions.Multiline);
}
