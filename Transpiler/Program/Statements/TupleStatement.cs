namespace Ronin.Transpiler.Program.Statements;

internal class TupleStatement : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        if (tokens.Length is < 5) throw new Parser.Exception("unexpected end of statement");
        int length = 1;
        while (tokens[length].Value is not Syntax.TupleEnd)
        {
            if (tokens[length].Kind is Token.Type.Identifier)
            {
                length += GetIdentifierLength(tokens[length..]);
            }
            if (tokens[length].Kind is Token.Type.Literal) ++length;
            if (tokens[length].Value is Syntax.Separator) ++length;
        }
        return length + 2;
    }
}
