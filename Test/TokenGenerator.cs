using Ronin.Compiler;
using Ronin.Lexicon;
using System.Reflection;

namespace Test;

internal class TokensGenerator
{
    public List<Token> Tokens { get; } = new();

    public TokensGenerator Add<T>(string source = "") where T : Token
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
            var symbol = typeof(T).GetField("symbol", BindingFlags.Public | BindingFlags.Static).GetValue(null) as string;
            Lexer lexer = new(symbol);
            token = constructor.Invoke(new object[] { lexer }) as T;            
        }
        
        Tokens.Add(token);
        return this;
    }

    public TokensGenerator Add<T0, T1>()
        where T0 : Token
        where T1 : Token
    {
        return Add<T0>().Add<T1>();
    }

    public TokensGenerator Add<T0, T1, T2>()
        where T0 : Token
        where T1 : Token
        where T2 : Token
    {
        return Add<T0>().Add<T1>().Add<T2>();
    }

    public TokensGenerator Add(Token token)
    {
        Tokens.Add(token);
        return this;
    }
}
