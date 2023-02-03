// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Union of <see cref="Hierarchy"/>, <see cref="Datum"/>, <see cref="Function"/>, 
///     <see cref="Datatype"/>, <see cref="Assignment"/>, <see cref="Reference"/>, and <see cref="Temporary"/>
/// </summary>
/// 
/// <remarks>
///     This is the central workhorse class for <see cref="Parser"/>
/// </remarks>
internal class Statement : Syntax, Compiler.IParsable<Statement>
{
    public static Statement Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Hierarchy.Parse(ref parser)
            ?? Function.Parse(ref parser)
            ?? Datatype.Parse(ref parser)
            ?? Assignment.Parse(ref parser)
            ?? Reference.Parse(ref parser)
            ?? Temporary.Parse(ref parser)
            ?? Datum.Parse(ref parser)
            ?? Unknown.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Statement { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Hierarchy(Statement statement) => statement.value as Hierarchy;
    public static implicit operator Datum(Statement statement) => statement.value as Datum;
    public static implicit operator Function(Statement statement) => statement.value as Function;
    public static implicit operator Datatype(Statement statement) => statement.value as Datatype;
    public static implicit operator Assignment(Statement statement) => statement.value as Assignment;
    public static implicit operator Reference(Statement statement) => statement.value as Reference;
    public static implicit operator Temporary(Statement statement) => statement.value as Temporary;
    public static implicit operator Unknown(Statement statement) => statement.value as Unknown;

    private Syntax value;
}