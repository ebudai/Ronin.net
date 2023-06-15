using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Error
{
    public static readonly List<Error> None = new();

    public Statement Statement { get; set; }
    public int Offset { get; init; }
}

[ExcludeFromCodeCoverage]
internal class DeveloperMistakeUnhandledSubclass<T> : Error { }