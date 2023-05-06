using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Error
{
    public Statement Statement { get; init; }
    public int Offset { get; init; }
}

[ExcludeFromCodeCoverage]
internal class UnknownSyntaxError : Error { }

[ExcludeFromCodeCoverage]
internal class DeveloperMistakeUnhandledSubclassException<T> : Exception
{
    public Statement Statement { get; init; }
    public int Offset { get; init; }
}