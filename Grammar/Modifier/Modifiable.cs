namespace Ronin.Grammar.Modifier;

public abstract class Modifiable : Syntax
{
    public void Apply<T>() where T : Modifier => modifiers.Add(Activator.CreateInstance<T>());
    public bool Is<T>() where T : Modifier => modifiers.Any(static modifier => modifier is T);

    public override int GetHashCode() => GetType().GetHashCode();

    private readonly HashSet<Modifier> modifiers = new();
}
