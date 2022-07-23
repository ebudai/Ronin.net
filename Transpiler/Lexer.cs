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
                                Kind = tokenTypes[regex],
                            });
                        }
                        column += match.Length;
                        parsed = true;
                        break;
                    }
                }

                if (!parsed)
                {
                    throw new Exception("unparsable token " + line[column..]);
                }
            }
        }
        return tokens.ToArray();
    }

    private const RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase;
    
    private static readonly Regex whitespace =      new(@"^\s+"                                     , options);
    private static readonly Regex strings =         new(@"^""[^""\\]*(\\.[^""\\]*)*"""              , options);
    private static readonly Regex characters =      new(@"^'\\?.'"                                  , options);
    private static readonly Regex unicodes =        new(@"^'\\u[a-f0-9]{4}'"                        , options);
    private static readonly Regex hexadecimals =    new(@"^0x[\d_a-f]+"                             , options);
    private static readonly Regex binaries =        new(@"^0b[01_]+"                                , options);
    private static readonly Regex floats =          new(@"^\d[\d_]*(([.][\d_]+(r(32)?)?)|r(32)?)"   , options);
    private static readonly Regex reals =           new(@"^\d[\d_]*([.][\d_])?[\d_]*r(16|64)"       , options);
    private static readonly Regex decimals =        new(@"^\$\d[\d_]*([.][\d_])?[\d_]*"             , options);
    private static readonly Regex integers =        new(@"^\d[\d_]*(i(8|16|32|64)?)?"               , options);
    private static readonly Regex symbols =         new(@"^[-\\~!@#%^&*=+,.;'/?:<>|""]"             , options);
    private static readonly Regex brackets =        new(@"^[\(\[{<>}\]\)]"                          , options);
    private static readonly Regex identifiers =     new(@"^[a-z_][a-z0-9_]*"                        , options);

    private static readonly Regex[] lexicalOrder =
    {
        whitespace,
        strings,
        characters,
        unicodes,
        hexadecimals,
        binaries,
        reals,
        floats,         
        decimals,
        integers, //TODO support 128 bit?
        symbols,
        brackets,
        identifiers,
    };

    private static readonly Dictionary<Regex, Token.Type> tokenTypes = new(ReferenceEqualityComparer.Instance)
    {
        { strings, Token.Type.Literal },
        { characters, Token.Type.Literal },
        { unicodes, Token.Type.Literal },
        { hexadecimals, Token.Type.Literal },
        { reals, Token.Type.Literal },
        { floats, Token.Type.Literal },
        { decimals, Token.Type.Literal },
        { integers, Token.Type.Literal },
        { symbols, Token.Type.Symbol },
        { brackets, Token.Type.Symbol },
        { identifiers, Token.Type.Identifier },
    };
}

