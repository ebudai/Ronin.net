// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

/// <summary>
///     Generalized parsing error used when no construct fits the given code
/// </summary>
internal class UnexpectedSyntaxError : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<UnexpectedSyntaxError>(ref context);
}
