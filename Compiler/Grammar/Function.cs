using Ronin.Token;

namespace Ronin.Grammar;

internal class Function : Syntax
{
    internal Identifier Name { get; } = new();
}
