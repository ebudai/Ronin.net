namespace Ronin.Transpiler.Grammar.Tokens;

internal abstract class Literal : Token
{
    public string Value = string.Empty;

    public static Literal[] GetLiterals() => typeof(Literal).Assembly.DefinedTypes
        .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Literal)))
        .Select(type => Activator.CreateInstance(type) as Literal)
        .ToArray();
}
