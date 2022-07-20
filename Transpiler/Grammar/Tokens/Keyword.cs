namespace Ronin.Transpiler.Grammar.Tokens;

internal abstract class Keyword : Token
{
    public static Keyword[] GetKeywords() => typeof(Token).Assembly.DefinedTypes
        .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Keyword)))
        .Select(type => Activator.CreateInstance(type) as Keyword)
        .ToArray();
}
