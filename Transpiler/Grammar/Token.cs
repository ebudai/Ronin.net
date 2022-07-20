using System.Reflection;
using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar;

internal abstract class Token
{
    protected internal const RegexOptions options = RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.Multiline;
    protected internal const BindingFlags binding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

    public abstract Regex[] Regexes { get; }

    internal int Line = 0;
    internal int Column = 0;
    internal int Indentation = 0;

    public Token Clone()
    {
        var token = Activator.CreateInstance(GetType()) as Token;
        token.Line = Line;
        token.Column = Column;
        token.Indentation = Indentation;

        foreach (var field in GetType().GetFields(binding))
        {
            field.SetValue(token, field.GetValue(this));
        }

        return token;
    }
}