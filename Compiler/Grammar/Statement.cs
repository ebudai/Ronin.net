// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Central workhorse class for <see cref="Parser"/>
/// </summary>
[GenerateOneOf]
internal partial class Statement : OneOfBase<Import, Function, Type, Datum, Association, Value, Scope, Unknown> { }
/*{
    public static Statement Parse(ref Parser current)
        => Export.Parse(ref current)
        ?? Import.Parse(ref current)
        ?? Function.Declaration.Parse(ref current)
        ?? Type.Declaration.Parse(ref current)
        ?? Datum.Declaration.Parse(ref current)
        ?? Association.Parse(ref current)
        ?? Value.Parse(ref current)
        ?? Scope.Parse(ref current)
        ?? Unknown.Parse(ref current) as Statement;
}*/