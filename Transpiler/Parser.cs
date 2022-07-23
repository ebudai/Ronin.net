using Ronin.Transpiler.Statements;

namespace Ronin.Transpiler;

internal class Parser
{
    public enum DeclarationContext { Global, Class, Function };

    public Statement[] Parse(ReadOnlySpan<Token> tokens)
    {
        List<Statement> statements = new();

        while (!tokens.IsEmpty)
        {
            Statement statement = tokens[0] switch
            {
                { Value: Syntax.DeclareVariable } => new DeclareVariable(ref tokens, this),
                { Kind: Token.Type.Literal } => new Literal(ref tokens),
                { Kind: Token.Type.Identifier } => tokens[1].Kind is Token.Type.Identifier ? new DeclareTypedVariable(ref tokens, this) : new Identifier(ref tokens),                
                _ => throw new Exception($"unknown token {tokens[0]}")
            };
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    public DeclarationContext Context { get; set; } = DeclarationContext.Global;

    public class Exception : System.Exception
    {
        public Exception(string message) : base(message) { }
    }
}
