using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal partial class Error
{
    public static readonly List<Error> None = new();
    
    public static List<Error> UnhandledSubclass<T>(Statement statement) => Errors<DeveloperMistakeUnhandledSubclass<T>>(statement);
    public static List<Error> ExportedScopeMustBeAnonymous(Statement statement) => Errors<ExportedScopeMustBeAnonymous>(statement);
    public static List<Error> ExportedScopeMustBeUnmodified(Statement statement) => Errors<ExportedScopeMustBeUnmodified>(statement);
    public static List<Error> ScopeAlreadyNamed(Statement statement) => Errors<ScopeAlreadyNamed>(statement);
    public static List<Error> UnknownSyntax(Statement statement) => Errors<UnknownSyntax>(statement);
    
    public Statement Statement { get; set; }
    public int Offset { get; init; }

    private static List<Error> Errors<T>(Statement statement) where T : Error, new() => new() { new T { Statement = statement } };
}

[ExcludeFromCodeCoverage] internal class DeveloperMistakeUnhandledSubclass<T> : Error { }
[ExcludeFromCodeCoverage] internal class UnknownSyntax : Error { }
[ExcludeFromCodeCoverage] internal class ExportedScopeMustBeAnonymous : Error { }
[ExcludeFromCodeCoverage] internal class ExportedScopeMustBeUnmodified : Error { }
[ExcludeFromCodeCoverage] internal class ScopeAlreadyNamed : Error { }