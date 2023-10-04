using Ronin.Grammar;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Import = Ronin.Grammar.Import;

namespace Ronin.Compiler;

internal class Error
{
    public Dictionary<string, object> Data { get; } = new();
    public string Reason { get; }
    public ReadOnlyMemory<Token> Tokens { get; protected init; }

    public Error(string reason) => Reason = reason;

    public void IsAbout(object data, [CallerArgumentExpression(nameof(data))] string name = "") => Data.Add(name, data);

    public static Error ScopeMustBeAnonymous(Context scope, Export export)
    {
        Error error = new("scope must be anonymous") { Tokens = new[] { export.Keyword } };
        error.IsAbout(scope);
        return error;
    }

    public static Error ScopeMustBeUnmodified(Context scope, Export export)
    {
        Error error = new("scope must be unmodified") { Tokens = new[] { export.Keyword } };
        error.IsAbout(scope);
        return error;
    }

    public static Error ScopeIsAlreadyPartOfAModule(Context scope, Export export)
    {
        Error error = new("scope is already a part of a module") { Tokens = new[] { export.Keyword } };
        error.IsAbout(scope);
        return error;
    }

    public static Error Redefinition(Context.Member member)
    {
        Error error = new("redefinition") { Tokens = member.Source };
        error.IsAbout(member);
        return error;
    }

    public static Error UnknownSyntax(Syntax unknown)
    {
        Error error = new("unknown syntax") { Tokens = unknown.Source };
        error.IsAbout(unknown);
        return error;
    }

    public static Error CouldNotResolve<T>(T member, Reference reference) where T : Syntax
    {
        Error error = new("could not resolve") { Tokens = reference.Source };
        error.IsAbout(member);
        return error;
    }

    public static Error UnresolvedImport(Import import)
    {
        Error error = new("unresolved import") { Tokens = import.Source };
        error.IsAbout(import);
        return error;
    }

    public static Error UnresolvedReference(Reference reference)
    {
        Error error = new("unresolved reference") { Tokens = reference.Source };
        error.IsAbout(reference);
        return error;
    }
}