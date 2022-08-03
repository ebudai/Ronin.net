namespace Ronin.Builder;

internal static class Builder
{
    /// <summary>
    /// Builds all .ronin files from given <paramref name="folder"/> (recursively) into a program
    /// </summary>
    /// <param name="folder">root of all .ronin files to be compiled</param>
    /// <returns>object representation of parsed code in <paramref name="folder"/></returns>
    public static Program Build(DirectoryInfo folder)
    {
        Program program = new();
        foreach (var file in folder.EnumerateFiles("*.ronin", SearchOption.AllDirectories))
        {
            int line = 0;
            int cursor = 0;
            var scope = Parse<Scope>("{" + File.ReadAllText(file.FullName) + "}", ref cursor, ref line) as Scope;
            scope.Parent = Scope.Global;
        }

        return program;
    }

    private static Syntax ParseLiteral(string sourcecode, ref int cursor, ref int line)
    {
        return Parse<Whitespace>(sourcecode, ref cursor, ref line)
            ?? Parse<TextLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<CharacterLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<UnicharLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<HexLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<BinaryLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<HalfPrecisionDecimalLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<DoublePrecisionDecimalLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<DecimalLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<MoneyLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<IntegerLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<DateTimeLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<DateLiteral>(sourcecode, ref cursor, ref line)
            ?? Parse<TimeLiteral>(sourcecode, ref cursor, ref line) as Syntax
            ?? throw new ParseException(line);
    }

    private static Scope ParseScope(string sourcecode, Scope parent, ref int cursor, ref int line)
    {
        Scope scope = new(parent);

        if (string.IsNullOrEmpty(sourcecode)) return scope;

        while (cursor <= sourcecode.Length)
        {
            Syntax syntax = Parse<Literal>(sourcecode, ref cursor, ref line)
                ?? Parse<Symbol>(sourcecode, ref cursor, ref line)
                ?? Parse<Identifier>(sourcecode, ref cursor, ref line)
                //?? ParseAggregate<ObjectLiteral>(source, ref cursor, ref line)
                ?? ParseObjectLiteral(sourcecode, ref cursor, ref line)
                ?? ParseListLiteral(sourcecode, ref cursor, ref line)// as Syntax
                ?? ParseParameterBlock(sourcecode, ref cursor, ref line)
                ?? ParseScope(sourcecode, scope, ref cursor, ref line) as Syntax
                ?? throw new ParseException("unparsable", line);
        }

        return scope;
    }

    /*private static T Parse<T>(string sourcecode, ref int cursor) where T : Syntax
    {
        var syntax = Activator.CreateInstance<T>();
        var match = syntax.Form.Match(sourcecode, cursor);
        if (match.Success && match.Index is 0)
        {
            cursor += match.Length;
            syntax.Value = match.Value;
            return syntax;
        }
        return null;
    }*/

    private static Syntax Parse<T>(string sourcecode, ref int cursor, ref int line) where T : Syntax
    {
        if (typeof(T) == typeof(Literal))
        {
            return Parse<TextLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<CharacterLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<UnicharLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<HexLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<BinaryLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<HalfPrecisionDecimalLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<DoublePrecisionDecimalLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<DecimalLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<MoneyLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<IntegerLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<DateTimeLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<DateLiteral>(sourcecode, ref cursor, ref line)
                ?? Parse<TimeLiteral>(sourcecode, ref cursor, ref line)
                ?? throw new ParseException(line);
        }
        var syntax = Activator.CreateInstance<T>();
        var match = syntax.Form.Match(sourcecode, cursor);
        if (match.Success && match.Index is 0)
        {
            cursor += match.Length;
            line += match.Value.Count(c => c == '\n');
            syntax.Value = match.Value;
            return syntax;
        }
        return null;
    }

    private static T Parse<T, U>(string sourcecode, ref int cursor, ref int line) where T : Aggregate<U> where U : Syntax
    {
        var aggregate = Activator.CreateInstance<T>();
        if (!sourcecode.StartsWith(aggregate.Start)) return null;
        var match = aggregate.Form.Match(sourcecode, cursor);
        if (match.Success && match.Index is 0)
        {
            line += match.Value.Count(c => c == '\n');
            aggregate.Value = match.Value;
            while (sourcecode[cursor..] != aggregate.End)
            {
                var member = Parse<U>(sourcecode, ref cursor, ref line) ?? throw new ParseException(line);
                aggregate.Members.Add(member as U);
            }
        }
        return aggregate;
    }

    private static T Parse<T, U, V>(string sourcecode, ref int cursor, ref int line)
        where T : Aggregate<U, V>
        where U : Syntax
        where V : Syntax
    {
        var literal = Activator.CreateInstance<T>();
        if (!sourcecode.StartsWith(literal.Start)) return null;
        var match = literal.Form.Match(sourcecode, cursor);
        if (match.Success && match.Index is 0)
        {
            line += match.Value.Count(c => c == '\n');
            literal.Value = match.Value;
            while (sourcecode[cursor..] != literal.End)
            {
                var code = literal.Value[1..^1];
                var member = Parse<U>(code, ref cursor, ref line) as Union<U, V>
                    ?? Parse<V>(code, ref cursor, ref line) as Union<U, V>
                    ?? throw new ParseException(line);
                literal.Members.Add(member);
            }
        }
        return literal;
    }

    /*private static Literal ParseLiteral(string sourcecode, ref int cursor, ref int line)
    {
        Literal literal = new();
        var match = literal.Match(sourcecode, cursor);
        if (match.Success && match.Index is 0)
        {
            cursor += match.Length;
            
            // if we passed-in "number" as the type, this indicates we have to determine the numeric type ourselves
            if (literal.Datatype is Language.Primitives.number)
            {
                literal.Datatype = literal switch
                {
                    HexLiteral => SmallestIntType(match.Value[2..], NumberStyles.AllowHexSpecifier | NumberStyles.AllowThousands),
                    BinaryLiteral => match.Value[2..].Length switch
                    {
                        <= 8 => Language.Primitives.@byte,
                        <= 16 => Language.Primitives.bits16,
                        <= 32 => Language.Primitives.bits32,
                        <= 64 => Language.Primitives.bits64,
                        _ => Language.Primitives.bigint
                    },
                    HalfPrecisionDecimalLiteral => Language.Primitives.dec16,
                    DoublePrecisionDecimalLiteral => Language.Primitives.dec64,
                    DecimalLiteral => Language.Primitives.@decimal,
                    IntegerLiteral => SmallestIntType(match.Value, NumberStyles.AllowThousands),
                    _ => throw new MalformedLiteralException(match.Value, line),
                };
                if (match.IsHexLiteral())
                {
                    literal.Datatype = SmallestIntType(match.Value[2..], NumberStyles.AllowHexSpecifier | NumberStyles.AllowThousands);
                }
                else if (match.IsBinaryLiteral())
                {
                    var value = match.Value[2..];
                    literal.Datatype = value.Length switch
                    {
                        <= 8 => Language.Primitives.@byte,
                        <= 16 => Language.Primitives.bits16,
                        <= 32 => Language.Primitives.bits32,
                        <= 64 => Language.Primitives.bits64,
                        _ => Language.Primitives.bitlist
                    };
                }
                else if (match.IsHalfLiteral())
                {
                    literal.Datatype = Language.Primitives.dec16;
                }
                else if (match.IsDoubleLiteral())
                {
                    literal.Datatype = Language.Primitives.dec64;
                }
                else if (match.IsDecimalLiteral())
                {
                    literal.Datatype = Language.Primitives.@decimal;
                }
                else
                {
                    literal.Datatype = SmallestIntType(match.Value, NumberStyles.AllowThousands);
                }
            }
            else if (literal.Datatype is Language.Primitives.text)
            {
                // it's possible there are newlines within the text.  count them
                line += literal.Value.Count(c => c == '\n');
            }
        }
        return null;
    }*/

    private static ObjectLiteral ParseObjectLiteral(string sourcecode, ref int cursor, ref int line)
    {
        ObjectLiteral literal = new();
        if (!sourcecode.StartsWith(literal.Start)) return null;
        var match = literal.Form.Match(sourcecode, cursor);
        if (match.Success && match.Index is 0)
        {
            line += match.Value.Count(c => c == '\n');
            literal.Value = match.Value;
            while (sourcecode[cursor..] != literal.End)
            {
                var member = ParseLiteral(literal.Value[1..^1], ref cursor, ref line)
                    ?? Parse<Identifier>(sourcecode, ref cursor, ref line)
                    ?? throw new ParseException(line);
                literal.Members.Add(member as Union<Literal, Identifier>);
            }
        }
        return literal;
    }

    private static ParameterBlock ParseParameterBlock(string sourcecode, ref int cursor, ref int line)
    {
        ParameterBlock parameters = new();
        if (!sourcecode.StartsWith(parameters.Start)) return null;
        var match = parameters.Form.Match(sourcecode, cursor);
        if (match.Success && match.Index is 0)
        {
            line += match.Value.Count(c => c == '\n');
            parameters.Value = match.Value;
            while (sourcecode[cursor..] != parameters.End)
            {
                var member = Parse<Instance>(sourcecode, ref cursor, ref line) ?? throw new ParseException(line);
                parameters.Members.Add(member);
            }
        }
        return parameters;
    }

    private static CollectionLiteral ParseListLiteral(string sourcecode, ref int cursor, ref int line)
    {
        CollectionLiteral literal = new();
        if (!sourcecode.StartsWith(literal.Start)) return null;
        var match = literal.Form.Match(sourcecode, cursor);
        if (match.Success && match.Index is 0)
        {
            line += match.Value.Count(c => c == '\n');
            literal.Value = match.Value;
            while (sourcecode[cursor..] != literal.End)
            {
                var member = ParseLiteral(literal.Value[1..^1], ref cursor, ref line) as Value
                    ?? Parse<Identifier>(sourcecode, ref cursor, ref line)
                    ?? throw new ParseException(line);
                literal.Members.Add(member);
            }
        }
        return literal;
    }
}
