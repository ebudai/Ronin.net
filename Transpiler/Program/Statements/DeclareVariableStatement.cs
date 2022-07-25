namespace Ronin.Transpiler.Program.Statements;

/*nternal class DeclareVariableStatement : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        if (tokens.Length is < 5) throw new Parser.Exception("unexpected end of statement");
        tokens = tokens[1..];

        int length = 1;
        string type = null;
        Statement initializer = null;

        var name = GetIdentifier(tokens, out var index);
        
        length += index;
        tokens = tokens[index..];

        // explicit
        if (tokens[0].Value is Syntax.TypeStart)
        {
            tokens = tokens[1..];
            ++length;
            type = GetIdentifier(tokens, out index);
            length += index;
            tokens = tokens[index..];
        }

        // with initializer
        if (tokens[0].Value is Syntax.Assign)
        {
            tokens = tokens[1..];
            ++length;
            initializer = parser.Parse(tokens, block, out index);
            length += index;
        }

        if (tokens[0].Value is Syntax.Terminal) length += 1;

        if (type is null && initializer is null) throw new Parser.Exception($"expecting {Syntax.TypeStart} or {Syntax.Assign}");

        block.Data.Add(name, new() { Type = new Block { Name = type }, Initializer = initializer });                

        return length;
    }
}
*/