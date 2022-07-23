using Ronin.Transpiler.Statements;

namespace Ronin.Transpiler;

internal class Parser
{
    public enum Context { Global, Class, Function };

    public Statement[] Parse(ReadOnlySpan<Token> tokens)
    {
        List<Statement> statements = new();

        while (!tokens.IsEmpty)
        {
            Statement statement = tokens[0] switch
            {
                { Value: Syntax.Implicit } => new DeclareImplicitVariable(ref tokens, this),
                { Kind: Token.Type.Literal} => new Literal(ref tokens),
                _ => throw new Exception($"unknown token {tokens[0].Value}")
            };
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    public Context CurrentContext { get; set; }

    public class Exception : System.Exception
    {
        public Exception(string message) : base(message) { }
    }




    //public static readonly object Anything = new();
    //public static readonly object Identifier = new();

    //private static readonly Statement DeclareImplicitVar = new(Implicit, Identifier, Assign, Anything, Terminal);

}
