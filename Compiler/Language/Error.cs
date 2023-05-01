using Ronin.Grammar;

namespace Ronin.Language;

internal class Error : Exception
{
    public Statement Statement { get; init; }
    public int Offset { get; init; }
}

internal class UnknownSyntaxError : Error { }

internal class UnhandledSubclassError<T> : Error where T : Syntax { }