// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

/// <summary>
///     Used when a <see cref="Parameter"/> or <see cref="Datatype"/> is supposed to specify a datatype or initializer and does not
/// </summary>
internal class UnspecifiedDatatypeError : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<UnspecifiedDatatypeError>(ref context);
}
