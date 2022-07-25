namespace Ronin.Transpiler.Program.Statements;

internal class DeclareTupleStatement : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        if (tokens.Length is < 5) throw new Parser.Exception("unexpected end of statement");
        tokens = tokens[1..];

        int index = 1;
        string type = null;
        Statement initializer = null;

        int cursor;
        var names = tokens[0].Value is Syntax.TupleStart 
            ? GetTupleIdentifiers(tokens, out cursor) 
            : new[] { GetIdentifier(tokens, out cursor) };
        
        index += cursor;
        tokens = tokens[cursor..];

        // explicit
        if (tokens[0].Value is Syntax.TypeStart)
        {
            tokens = tokens[1..];
            ++index;
            if (tokens[0].Value is Syntax.TupleStart)
            {
                type = Syntax.TupleStart + string.Join(",", GetTupleIdentifiers(tokens, out cursor)) + Syntax.TupleEnd;
            }
            else
            {
                type = GetIdentifier(tokens, out cursor);
            }
            
            index += cursor;
            tokens = tokens[cursor..];
        }

        // with initializer
        if (tokens[0].Value is Syntax.Assign)
        {
            tokens = tokens[1..];
            ++index;
            initializer = parser.Parse(tokens, block, out cursor);
            index += cursor;
        }

        if (tokens[0].Value is Syntax.Terminal) index += 1;

        if (type is null && initializer is null) throw new Parser.Exception($"expecting {Syntax.TypeStart} or {Syntax.Assign}");

        foreach (var name in names)
        {
            block.Data.Add(name, new() { Type = new Block { Name = type }, Initializer = initializer });
        }

        return index;
    }
}
