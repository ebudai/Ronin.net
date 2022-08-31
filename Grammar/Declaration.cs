namespace Ronin.Grammar;

public class Declaration : Syntax
{
    public Declaration(string name) => Modifiers.Add(name);

    public List<string> Modifiers { get; } = new();

    public int Length => Modifiers.Sum(name => name.Length);
}
