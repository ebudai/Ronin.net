namespace Ronin.Grammar;

public class Expression : Syntax
{
    public List<Syntax> Syntax { get; } = new();

    public bool IsEmpty => Syntax.Count is 0;
    public bool IsScopeClose { get; set; }
}
