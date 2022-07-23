using Ronin.Transpiler.Statements;

namespace Ronin.Transpiler;

internal class Parser
{
    public Statement[] Parse(ReadOnlySpan<Token> tokens)
    {
        List<Statement> statements = new();

        while (!tokens.IsEmpty)
        {
            Statement statement = tokens[0] switch
            {
                { Value: Syntax.DeclareVariable } => tokens.IsBefore(Syntax.DeclareVariableTypeStart, Syntax.Terminal) 
                    ? new DeclareVariableExplicit(ref tokens, this) 
                    : new DeclareVariableImplicit(ref tokens, this),
                { Kind: Token.Type.Literal } => new Literal(ref tokens),
                { Kind: Token.Type.Identifier } => new Identifier(ref tokens),                
                _ => throw new Exception($"unknown token {tokens[0]}")
            };
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    public Scope CurrentScope { get; } = GlobalScope;

    public static Scope GlobalScope = new(string.Empty, new(256));

    public class Exception : System.Exception
    {
        public Exception(string message) : base(message) { }
    }

    public record Scope(string Name, List<Statement> Statements);
}
