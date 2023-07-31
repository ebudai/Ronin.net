using Ronin.Grammar;
using System.Runtime.CompilerServices;

namespace Ronin.Compiler;

internal class Error
{
    public class Message
    {
        public const string ScopeMustBeAnonymous = "scope must be anonymous";
        public const string ScopeMustBeUnmodified = "scope must be unmodified";
        public const string ScopeIsAlreadyPartOfModule = "scope is already a part of a module";
        public const string Redefinition = "redefinition";
        public const string UnknownSyntax = "unknown syntax";
    }

    public Dictionary<string, object> Data { get; } = new();
    public string Reason { get; }
    public ReadOnlyMemory<Lexicon.Token> Tokens { get; protected init; }

    public Error(string reason) => Reason = reason;

    public void IsAbout(object data, [CallerArgumentExpression(nameof(data))] string name = "") => Data.Add(name, data);

    public static Error ScopeMustBeAnonymous(Definition scope, Join export)
    {
        Error error = new(Message.ScopeMustBeAnonymous) { Tokens = new[] { export.Keyword } };
        error.IsAbout(scope);
        return error;
    }

    public static Error ScopeMustBeUnmodified(Definition scope, Join export)
    {
        Error error = new(Message.ScopeMustBeUnmodified) { Tokens = new[] { export.Keyword } };
        error.IsAbout(scope);
        return error;
    }

    public static Error ScopeIsAlreadyPartOfAModule(Definition scope, Join export)
    {
        Error error = new(Message.ScopeIsAlreadyPartOfModule) { Tokens = new[] { export.Keyword } };
        error.IsAbout(scope);
        return error;
    }

    public static Error Redefinition(Definition.Member member, Identifier identifier)
    {
        Error error = new(Message.Redefinition) { Tokens = identifier.Source };
        error.IsAbout(member);
        return error;
    }

    public static Error UnknownSyntax(Unknown unknown)
    {
        Error error = new(Message.UnknownSyntax) { Tokens = unknown.Source };
        error.IsAbout(unknown);
        return error;
    }
}