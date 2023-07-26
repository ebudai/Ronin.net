using Ronin.Grammar;
using System.Runtime.CompilerServices;

namespace Ronin.Compiler;

internal class Error
{
    public Dictionary<string, object> Data { get; } = new();
    public string Reason { get; }
    public ReadOnlyMemory<Lexicon.Token> Tokens { get; protected init; }

    public Error(string reason) => Reason = reason;

    public void IsAbout(object data, [CallerArgumentExpression(nameof(data))] string name = "") => Data.Add(name, data);

    public static Error UnhandledSubclass<T>(Type type)
    {
        Error error = new("developer mistake");
        Type parent = typeof(T);
        error.IsAbout(parent);
        error.IsAbout(type);
        return error;
    }

    public static Error ScopeMustBeAnonymous(Definition scope, Export export)
    {
        Error error = new("scope must be anonymous") { Tokens = export.Source[..1] };
        error.IsAbout(scope);
        return error;
    }

    public static Error ScopeMustBeUnmodified(Definition scope, Modifiers modifiers)
    {
        Error error = new("scope must be unmodified") { Tokens = modifiers.Source };
        error.IsAbout(scope);
        return error;
    }

    public static Error ScopeIsAlreadyPartOfAModule(Definition scope, Name name)
    {
        Error error = new("scope is already a part of a module") { Tokens = name.Source };
        error.IsAbout(scope);
        return error;
    }

    public static Error Redefinition(Definition.Member element, ReadOnlyMemory<Lexicon.Token> tokens)
    {
        Error error = new("redefinition") { Tokens = tokens };
        error.IsAbout(element);
        return error;
    }

    public static Error UnknownSyntax(Unknown unknown)
    {
        Error error = new("unknown syntax") { Tokens = unknown.Source };
        error.IsAbout(unknown);
        return error;
    }
}