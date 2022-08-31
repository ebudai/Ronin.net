namespace Ronin.Grammar.Aggregates;

internal class Scope : Aggregate<Expression>
{
    internal void Import(Scope scope) => _imports.Add(scope);

    private readonly List<Scope> _imports = new();
}
