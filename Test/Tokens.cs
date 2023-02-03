using Ronin.Compiler;
using Ronin.Lexicon;
using System.Reflection;

namespace Test;

internal class Tokens
{
    public Token[] ToArray() => tokens.Append(Sentinel.Instance).ToArray();

    public Tokens Add<T>(string source = "") where T : Token
    {
        Token token;
        var constructor = typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Lexer), typeof(int) });
        if (constructor is not null)
        {
            Lexer lexer = new(source);
            token = constructor.Invoke(new object[] { lexer, source.Length }) as T;
        }
        else
        {
            constructor = typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Lexer) });
            var value = typeof(T).GetField("symbol", BindingFlags.Public | BindingFlags.Static)
                ?? typeof(T).GetField("keyword", BindingFlags.Public | BindingFlags.Static);
            Lexer lexer = new(value.GetValue(null) as string);
            token = constructor.Invoke(new object[] { lexer }) as T;
        }

        tokens.Add(token);
        return this;
    }

    public Tokens Add(Token token)
    {
        tokens.Add(token);
        return this;
    }

    private readonly List<Token> tokens = new();
}
