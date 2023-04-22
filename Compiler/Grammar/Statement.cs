// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     Central workhorse class for <see cref="Parser"/>
/// </summary>
internal class Statement : CompositeSyntax<Statement, ImportExport, Assignment, Function, Datatype, Scope, Interval, Value, Datum, Unknown>
{

}