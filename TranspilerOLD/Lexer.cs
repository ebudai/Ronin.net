namespace Ronin.Transpiler;

internal static class Lexer
{
    internal static Token[] Lex(string[] lines)
    {
        List<Token> tokens = new(1 << 16);
        for (int lineNumber = 0, max = lines.Length; lineNumber != max; ++lineNumber)
        {
            string line = lines[lineNumber];
            int column = 0;

            while (column < line.Length)
            {
                var parsed = false;
                var remaining = line[column..];
                foreach (var regex in Syntax.LexicalOrder)
                {
                    var match = regex.Match(remaining);
                    if (match.Success)
                    {
                        if (!string.IsNullOrWhiteSpace(match.Value))
                        {
                            tokens.Add(new()
                            {
                                Value = match.Value,
                                Column = column,
                                Line = lineNumber,
                                Kind = Syntax.TokenTypes[regex],
                            });
                        }
                        column += match.Length;
                        parsed = true;
                        break;
                    }
                }

                if (!parsed)
                {
                    throw new Parser.Exception("unparsable token " + line[column..]);
                }
            }
        }
        return tokens.ToArray();
    }
}