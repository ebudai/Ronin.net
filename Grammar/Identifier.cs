using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Ronin.Grammar;

[DebuggerDisplay("{ToString()}")]
public class Identifier : Syntax
{
    public List<string> Names { get; } = new();
    public Dictionary<int, Expression> Parameters { get; } = new();
    public Datatype ReturnType { get; set; }

    public Identifier() { }

    public Identifier(string name) => Names.Add(formatter.Replace(name,  " "));



    private static readonly Regex formatter = new(@"\s+", RegexOptions.Multiline);

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        var name = string.Empty;
        if (Parameters.Count is 0) return string.Join(' ', Names);
        var max = Math.Max(Names.Count, Parameters.Keys.Max());

        for (int i = 0; i <= max; ++i)
        {
            if (Parameters.ContainsKey(i)) name += $"({string.Join("", Parameters[i])}) ";
            if (i < Names.Count) name += Names[i] + " ";
        }
        return name.TrimEnd();
    }
}
