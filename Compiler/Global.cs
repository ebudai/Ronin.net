using Ronin.Grammar;

namespace Ronin.Compiler;

internal class Global
{
    public static Definition Scope => scope.Definition;

    private static readonly Scope scope = new AnonymousScope { Definition = new() };
}
