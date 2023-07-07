using Ronin.Grammar;

namespace Ronin.Language;

internal partial class Error
{
    public static readonly List<Error> None = new();
    public static List<Error> UnhandledSubclass<T>(Statement statement) => new() { new DeveloperMistakeUnhandledSubclass<T> { Statement = statement } };

    public Statement Statement { get; set; }
    public int Offset { get; init; }
}

internal class DeveloperMistakeUnhandledSubclass<T> : Error { }