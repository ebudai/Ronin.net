namespace Ronin.Transpiler.Program.Statements;

internal class LiteralStatement : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        if (tokens.Length is < 2) throw new Parser.Exception("unexpected end of statement");
        return 2;
    }
}
