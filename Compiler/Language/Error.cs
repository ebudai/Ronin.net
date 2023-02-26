using Ronin.Grammar;

namespace Ronin.Language;

internal class Error
{
    public Statement Statement { get; init; }
    public int Offset { get; init; }
}

internal class UnknownSyntaxError : Error { }