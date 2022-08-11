using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Ronin.Parser.Grammar;

[DebuggerDisplay("{ToString()}")]
internal class Identifier : Syntax
{
    internal Identifier() { }

    internal Identifier(string name) => names.Add(formatter.Replace(name,  " "));

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        var name = string.Empty;
        if (parameters.Count is 0) return string.Join(' ', names);
        var max = Math.Max(names.Count, parameters.Keys.Max());

        for (int i = 0; i <= max; ++i)
        {
            if (parameters.ContainsKey(i)) name += $"({string.Join("", parameters[i])}) ";
            if (i < names.Count) name += names[i] + " ";
        }
        return name.TrimEnd();
    }

    internal new static Identifier Parse(Context context)
    {
        var lexed = context.Lex(identifier);
        return lexed is null ? null : new(lexed);
    }

    internal bool TryAdd(Syntax syntax, Context context)
    {
        if (syntax is Identifier identifier)
        {
            names.AddRange(identifier.names);
        }
        else if (syntax is Expression expression)
        {
            parameters.Add(names.Count, expression);
        }
        else
        {
            if (!parameters.TryGetValue(names.Count, out expression))
            {
                expression = new();
                parameters.Add(names.Count, expression);
            }
            return expression.TryAdd(syntax, context);
        }
        return true;
    }

    private readonly List<string> names = new();
    private readonly Dictionary<int, Expression> parameters = new();

    private static readonly Regex formatter = new(@"\s+", RegexOptions.Multiline);
}
