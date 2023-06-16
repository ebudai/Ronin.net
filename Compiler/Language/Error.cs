using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal partial class Error
{
    public static readonly List<Error> None = new();
    public static List<Error> UnhandledSubclass<T>(Statement statement) => new() { new DeveloperMistakeUnhandledSubclass<T> { Statement = statement } };

    public Statement Statement { get; set; }
    public int Offset { get; init; }
}

[ExcludeFromCodeCoverage]
internal class DeveloperMistakeUnhandledSubclass<T> : Error { }