namespace Ronin.Transpiler.Statements;

internal class DeclareTypedVariable : Statement
{
    public DeclareTypedVariable(ref ReadOnlySpan<Token> tokens, Parser parser) : base(tokens[0])
    {

    }
}
