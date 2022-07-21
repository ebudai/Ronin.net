using System.Text.RegularExpressions;

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
                foreach (var regex in lexicalOrder)
                {
                    var match = regex.Match(line[column..]);
                    if (match.Success)
                    {
                        if (!string.IsNullOrWhiteSpace(match.Value))
                        {
                            tokens.Add(new()
                            {
                                Value = match.Value,
                                Column = column,
                                Line = lineNumber,
                            });
                        }
                        column += match.Length;
                        parsed = true;
                        break;
                    }
                }

                if (!parsed) throw new Exception("unparsable token " + line[column..]);
            }
        }
        return tokens.ToArray();
    }

    private const RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.ExplicitCapture;
    
    private static readonly Regex whitespace =          new(@"^\s+"                             , options);
    private static readonly Regex stringLiterals =      new(@"^""[^""\\]*(\\.[^""\\]*)*"""      , options);
    private static readonly Regex characterLiterals =   new(@"^'\\?.'"                          , options);
    private static readonly Regex unicodeLiterals =     new(@"^'\\u[a-fA-F0-9]{4}'"             , options);
    private static readonly Regex hexLiterals =         new(@"^0[xX][\d_a-fA-F]+"               , options);
    private static readonly Regex binaryLiterals =      new(@"^0[bB][01_]+"                     , options);
    private static readonly Regex integerLiterals =     new(@"^\d[\d_]*[uU]?[lL]?"              , options);
    private static readonly Regex floatLiterals =       new(@"^\d[\d_]*[.]?[\d_]*[fF]"          , options);
    private static readonly Regex doubleLiterals =      new(@"^\d[\d_]*([.][\d_]*[dD]?)|[dD]"   , options);
    private static readonly Regex decimalLiterals =     new(@"^\d[\d_]*([.][\d_])?[\d_]*[mM]"   , options);
    private static readonly Regex symbols =             new(@"^[-\\~!@#%^&*=+,.;'/?:<>|""]"     , options);
    private static readonly Regex brackets =            new(@"^\(\[{<>}\]\)"                    , options);
    private static readonly Regex keywords =            new(@"var|type"                         , options);
    private static readonly Regex identifiers =         new(@"^[A-Za-z_][A-Za-z0-9_]*"          , options);
    
    private static readonly Regex[] lexicalOrder =
    {
        whitespace,
        stringLiterals,
        characterLiterals,
        unicodeLiterals,
        hexLiterals,
        binaryLiterals,        
        floatLiterals,
        doubleLiterals, 
        decimalLiterals,
        integerLiterals, //TODO support 128 bit?
        symbols,
        brackets,
        keywords,
        identifiers,
    };
}

