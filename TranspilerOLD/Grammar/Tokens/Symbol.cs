namespace Ronin.Transpiler.Grammar.Tokens;

internal abstract class Symbol : Token
{
    public static Symbol[] GetSymbols() => typeof(Token).Assembly.DefinedTypes
        .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Symbol)))
        .Select(type => Activator.CreateInstance(type) as Symbol)
        .ToArray();
}
