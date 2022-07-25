namespace Ronin.Transpiler.Program.Statements;

internal class TupleStatement : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        if (tokens.Length is < 5) throw new Parser.Exception("unexpected end of statement");
        int index = 1;
        while (tokens[index].Value is not Syntax.TupleEnd)
        {
            if (tokens[index].Kind is Token.Type.Identifier)
            {
                GetIdentifier(tokens[index..], out var cursor);
                index += cursor;
            }
            if (tokens[index].Kind is Token.Type.Literal) ++index;
            if (tokens[index].Value is Syntax.Separator) ++index;
        }
        return index + 2;
    }
}
