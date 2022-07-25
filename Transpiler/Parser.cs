using Ronin.Transpiler.Program;
using System.Text.RegularExpressions;

namespace Ronin.Transpiler;

internal class Parser
{
    public Statement Parse(ReadOnlySpan<Token> tokens, Block parent) => Parse(tokens, parent, out var _);

    public Statement Parse(ReadOnlySpan<Token> tokens, Block parent, out int index)
    {
        index = 0;
        string signature = string.Empty;
        Regex replace = new(@"I<.+>", RegexOptions.Compiled);
        while (!tokens.IsEmpty)
        {
            if (index == tokens.Length) throw new Exception("unexpected end of statement");
            signature += tokens[index];
            signature = replace.Replace(signature, "II");
            foreach (var regex in Syntax.ParseOrder)
            {
                var match = regex.Match(signature);
                if (match.Success)
                {
                    var statement = Syntax.Generators[regex](tokens, parent, this);
                    index = statement.Length;
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
