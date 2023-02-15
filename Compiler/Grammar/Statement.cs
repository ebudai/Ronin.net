// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

/// <summary>
///     Union of <see cref="Hierarchy"/>, <see cref="Datum"/>, <see cref="Function"/>, 
///     <see cref="Datatype"/>, <see cref="Assignment"/>, <see cref="Reference"/>, <see cref="Scalar"/>, 
///     <see cref="Arguments"/>, <see cref="Scope"/>, <see cref="Datum"/> and <see cref="Unknown"/>
/// </summary>
/// 
/// <remarks>
///     This is the central workhorse class for <see cref="Parser"/>
/// </remarks>
internal class Statement : Syntax, Compiler.IParsable<Statement>
{
    public Syntax Syntax { get; init; }

    public static Statement Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Hierarchy.Parse(ref parser)
            ?? Assignment.Parse(ref parser)
            ?? Function.Parse(ref parser)
            ?? Datatype.Parse(ref parser)
            ?? Scope.Parse(ref parser)
            ?? Interval.Parse(ref parser)
            ?? Value.Parse(ref parser)            
            ?? Datum.Parse(ref parser)
            ?? Unknown.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Statement { Syntax = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Hierarchy(Statement statement) => statement.Syntax as Hierarchy;
    public static implicit operator Function(Statement statement) => statement.Syntax as Function;
    public static implicit operator Datatype(Statement statement) => statement.Syntax as Datatype;
    public static implicit operator Assignment(Statement statement) => statement.Syntax as Assignment;
    public static implicit operator Interval(Statement statement) => statement.Syntax as Interval;
    public static implicit operator Value(Statement statement) => statement.Syntax as Value;
    public static implicit operator Scope(Statement statement) => statement.Syntax as Scope;
    public static implicit operator Datum(Statement statement) => statement.Syntax as Datum;
    public static implicit operator Unknown(Statement statement) => statement.Syntax as Unknown;    
}