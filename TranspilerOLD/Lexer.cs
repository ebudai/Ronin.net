using Ronin.Transpiler.Tokens;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ronin.Transpiler;

internal class Lexer
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

    internal Token[] Lex(string code)
    {
        
    }

    internal static Token[] TokenizeLiterals(Token[] tokens)
    {
        const RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline;

        List<Token> tokens = new(256);

        Regex strings = new(@"^""(?<Value>.+?)[^\\]""", options);
        var matches = strings.Matches(code);

        foreach (var match in matches)
        {
            StringLiteral literal = 
        }
        return tokens.ToArray();
    }

    private static T Populate<T>(Match match) where T : class, new()
    {
        const BindingFlags binding = BindingFlags.Public | BindingFlags.Instance;

        var populated = new T();
        List<FieldInfo> fields = new(16);
        
        for (int group = 1, maxGroup = match.Groups.Count; group <= maxGroup; ++group)
        {
            var field = typeof(T).GetField(match.Groups[group].Name, binding);
            if (field is null) break;
            fields.Add(field);
        }
        
        if (fields.Count != typeof(T).GetFields(binding).Length) return populated;

        foreach (var field in fields)
        {
            field.SetValue(populated, match.Groups[field.Name].Value);
        }

        return populated;
    }



    /*internal static Token[] Lex(string[] lines)
    {
        const BindingFlags binding = BindingFlags.Public | BindingFlags.Instance;

        List<Token> tokens = new(256);
        List<FieldInfo> fields = new(16);

        for (int line = 0, maxLine = lines.Length; line != maxLine; ++line)
        {
            int column = 0;
            int indentation = 0;

            ref string words = ref lines[line];            

            while (column < words.Length)
            {
                var whatsLeft = words[column..];

                for (int index = 0, max = Tokens.Count; index != max; ++index)
                {
                    var token = Tokens[index];

                    if (token is Identifier)
                    {
                        int i = 3;
                    }
                    // if the keyword does not apply here, it is instead an Identifier
                    if (token is Keyword keyword && !keyword.Applies.HasFlag(LexicalScope.Global))
                    {
                        // lame
                        var patternField = typeof(Regex).GetField("pattern", binding);
                        var pattern = patternField.GetValue(keyword) as string;
                        token = new Identifier() { Value = pattern };
                    }
                    
                    var matches = token.Regexes.Select(regex => regex.Match(whatsLeft)).Where(match =>
                    {
                        if (!match.Success) return false;
                        // make sure we matched the whole thing
                        // this is for cases where we have things like 'returnable',
                        // so we don't match on 'return'
                        var index = whatsLeft.IndexOf(' ');
                        if (index is -1) index = whatsLeft.Length;
                        var value = match.Value;
                        if (token is not Whitespace) value = value.Trim();
                        return value.Length == whatsLeft[..index].Length;
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
                    if (token is Whitespace whitespace)
                    {
                        indentation = whitespace.Spaces.Length;
                    }
                    else
                    {
                        tokens.Add(token.Clone());
                        indentation = 0;
                    }
                    token.Indentation = indentation;

                    foreach (var field in fields)
                    {
                        field.SetValue(token, match.Groups[field.Name].Value);
                    }

                    column += match.Length;

                    if (token is Unparsable)
                    {
                        int i = 3;
                    }


                    break;
                }
            }
        }

        return tokens.ToArray();
    }*/
}
