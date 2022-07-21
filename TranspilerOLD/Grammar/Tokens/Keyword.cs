using Ronin.Transpiler.Grammar.Flags;

namespace Ronin.Transpiler.Grammar.Tokens;

internal abstract class Keyword : Token
{
    public abstract LexicalScope Applies { get; }

    public static Keyword[] GetKeywords() => typeof(Token).Assembly.DefinedTypes
        .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Keyword)))
        .Select(type => Activator.CreateInstance(type) as Keyword)
        .ToArray();
}
