namespace Ronin.Grammar;

public class Declaration : Syntax
{
    public Declaration(string name) => Names.Add(name);

    public List<string> Names { get; } = new();

    public string Name => string.Join(' ', Names.Select(name => name.Trim()));

    public int Length => Names.Sum(name => name.Length);
}
