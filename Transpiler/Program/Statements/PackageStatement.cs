namespace Ronin.Transpiler.Program.Statements;

internal class PackageStatement : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        if (tokens.Length is < 2) return tokens.Length;
        tokens = tokens[1..];
        block.Name = GetIdentifier(tokens, out var index);
        if (tokens[index].Value is not Syntax.Terminal) throw new Parser.Exception($"expecting {Syntax.Terminal}");
        return index + 2;
    }
}
