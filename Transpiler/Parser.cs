using Ronin.Transpiler.Program;

namespace Ronin.Transpiler;

internal class Parser
{
    public Statement Parse(ReadOnlySpan<Token> tokens, Block parent) => Parse(tokens, parent, out var _);

    public Statement Parse(ReadOnlySpan<Token> tokens, Block parent, out int index)
    {
        index = 0;
        string signature = string.Empty;
        while (!tokens.IsEmpty)
        {
            if (index == tokens.Length) throw new Exception("unexpected end of statement");
            signature += tokens[index];
            foreach (var regex in Syntax.ParseOrder)
            {   
                var match = regex.Match(signature);
                if (match.Success)
                {
                    var statement = Activator.CreateInstance(Syntax.StatementTypes[regex]) as Statement;
                    index = statement.Construct(tokens, parent, this);
                    return statement;
                }
            }
            ++index;
        }

        throw new Exception("unexpected end of file");
    }

    public class Exception : System.Exception
    {
        public Exception(string message) : base(message) { }
    }
}
