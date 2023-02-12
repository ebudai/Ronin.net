using Ronin.Compiler;
using Ronin.Lexicon;
using System.Reflection;

namespace Test;

internal class Tokens
{
    public Token[] ToArray() => tokens.Append(Sentinel.Instance).ToArray();

    public Tokens Add<T>(string source = "") where T : Token, new()
    {
        var field = typeof(T).GetField("symbol", BindingFlags.Public | BindingFlags.Static);
        var sourcecode = field is null ? source : field.GetValue(null) as string;
        tokens.Add(new T { Sourcecode = sourcecode.ToArray() });
        return this;
    }

    public Tokens Add(Token token)
    {
        tokens.Add(token);
        return this;
    }

    private readonly List<Token> tokens = new();
}
