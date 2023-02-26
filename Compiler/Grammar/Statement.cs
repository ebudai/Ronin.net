// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

/// <summary>
///     Central workhorse class for <see cref="Parser"/>
/// </summary>
internal class Statement : CompositeSyntax<Statement, ImportExportSyntax, AssignmentSyntax, FunctionDeclarationSyntax, DatatypeDeclarationSyntax, Scope, IntervalSyntax, Value, DatumDeclarationSyntax, UnknownSyntax>
{

}