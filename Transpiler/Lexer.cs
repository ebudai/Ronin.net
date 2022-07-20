using Ronin.Transpiler.Grammar;
using Ronin.Transpiler.Grammar.Tokens;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ronin.Transpiler;

internal static class Lexer
{
    // order matters - first one to match wins
    private static readonly List<Token> Tokens = new(64);
    
    static Lexer() 
    {
        Tokens.Add(new Whitespace());

        Tokens.AddRange(Symbol.GetSymbols());
        Tokens.AddRange(Operator.GetOperators());
        Tokens.AddRange(Literal.GetLiterals());
        Tokens.AddRange(Keyword.GetKeywords());

        Tokens.Add(new Identifier());

        Tokens.Add(new Unparsable());
    }

    internal static Token[] Lex(string[] lines)
    {
        const BindingFlags binding = BindingFlags.Public | BindingFlags.Instance;

        List<Token> tokens = new(256);
        List<FieldInfo> fields = new(16);

        for (int line = 0, maxLine = lines.Length; line != maxLine; ++line)
        {
            int column = 0;
            ref string words = ref lines[line];
            
            while (column < words.Length)
            {
                var whatsLeft = words[column..];

                for (int index = 0, max = Tokens.Count; index != max; ++index)
                {
                    var token = Tokens[index];

                    // if the keyword does not apply here, it is instead an Identifier
                    if (token is Keyword keyword && !keyword.Applies.HasFlag(LexicalScope.Global))
                    {
                        // lame
                        var patternField = typeof(Regex).GetField("pattern", BindingFlags.NonPublic | BindingFlags.Instance);
                        var pattern = patternField.GetValue(keyword) as string;
                        token = new Identifier() { Value = pattern };
                    }
                    
                    var matches = token.Regexes.Select(regex => regex.Match(whatsLeft)).Where(match =>
                    {
                        if (!match.Success) return false;
                        // make sure we matched the whole thing
                        var index = whatsLeft.IndexOf(' ');
                        if (index is -1) index = whatsLeft.Length;
                        return match.Value.Trim().Length == whatsLeft[..index].Length;
                    }).ToArray();

                    if (matches.Length is 0) continue;
                    var match = matches[0];
                    
                    fields.Clear();
                    
                    for (int group = 1, maxGroup = match.Groups.Count; group <= maxGroup; ++group)
                    {
                        var field = token.GetType().GetField(match.Groups[group].Name, binding);
                        if (field is null) break;
                        fields.Add(field);
                    }

                    if (fields.Count != token.GetType().GetFields(binding).Length) continue;

                    token.Line = line;
                    token.Column = column;
                    foreach (var field in fields)
                    {
                        field.SetValue(token, match.Groups[field.Name].Value);
                    }
                    if (token is Whitespace whitespace)
                    {
                        token.Indentation = whitespace.Spaces.Length;
                    }
                    else
                    {
                        tokens.Add(token.Clone());
                        token.Indentation = 0;
                    }
                    column += match.Length;
                    break;
                }
            }
        }

        return tokens.ToArray();
    }
}
