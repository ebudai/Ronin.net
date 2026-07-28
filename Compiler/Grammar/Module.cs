using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal class Module
{
    public List<Scope> Scopes { get; } = [];

    public Module() { }
    public Module(Scope scope) => Scopes.Add(scope);

    public static Module Parse(ref Parser current)
    {
        Parser parser = current;

        Scope scope = new();

        while (Statement.Parse(ref parser) is Statement statement)
        {
            scope.Statements.Add(statement);
            parser.TryAdvance<Terminal>();
        }

        current = parser;
        return new Module(scope);
    }

    public class Unresolved : Module
    {
        public Name Name { get; init; }
    }
}
