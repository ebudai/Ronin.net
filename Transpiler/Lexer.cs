using Ronin.Transpiler.Grammar;
using Ronin.Transpiler.Grammar.Tokens;
using Ronin.Transpiler.Grammar.Tokens.Keywords;
using Ronin.Transpiler.Grammar.Tokens.Literals;
using Ronin.Transpiler.Grammar.Tokens.Operators;
using Ronin.Transpiler.Grammar.Tokens.Symbols;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ronin.Transpiler;

internal static class Lexer
{
    internal static Token[] GetTokens(string[] lines)
    {
        const BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        List<Token> tokens = new(256);
        List<PropertyInfo> properties = new(16);

        for (int line = 0, maxLine = lines.Length; line != maxLine; ++line)
        {
            int column = 0;
            ref string words = ref lines[line];
            
            while (column < words.Length)
            {
                foreach (var (regex, tokenType) in TokenMatchers_)
                {
                    var chunk = words[column..];
                    var match = regex.Match(chunk);
                    if (!match.Success) continue;
                    var index = chunk.IndexOf(' ');
                    if (index is -1) index = chunk.Length;
                    if (match.Value.Trim().Length != chunk[..index].Length) continue;

                    properties.Clear();
                    
                    for (int group = 1, maxGroup = match.Groups.Count; group <= maxGroup; ++group)
                    {
                        var property = tokenType.GetProperty(match.Groups[group].Name, binding);
                        if (property is null) break;
                        properties.Add(property);
                    }

                    if (properties.Count != tokenType.GetProperties(binding).Length) continue;

                    var token = Activator.CreateInstance(tokenType) as Token;
                    token.Line = line;
                    token.Column = column;
                    foreach (var property in properties)
                    {
                        property.SetValue(token, match.Groups[property.Name].Value);
                    }
                    tokens.Add(token);
                    column += match.Length;
                    break;
                }
            }
        }

        return tokens.ToArray();
    }

    private const RegexOptions Options = RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.Multiline;

    // order matters - first one to match wins
    private static readonly List<(Regex regex, Type tokenType)> TokenMatchers_ = new()
    {
        (new(@"^(?<Spaces>\s+)"                         , Options), typeof(Whitespace)),
        (new(@"^'(?<Value>\\?.)'\s*"                    , Options), typeof(CharLiteral)),
        (new(@"^'(?<Value>\\[uU][a-fA-F0-9]{4})'\s*"    , Options), typeof(CharLiteral)),
        (new(@"^""(?<Value>.+?)[^\\]""\s*"              , Options), typeof(StringLiteral)),
        (new(@"^(?<Value>true|false)\s*"                , Options), typeof(BooleanLiteral)),
        (new(@"^(?<Value>-?[\d_]+[uU]?[lL]?)\s*"        , Options), typeof(NumericLiteral)),
        (new(@"^(?<Value>-?0[xX][\d_a-fA-F]+)\s*"       , Options), typeof(NumericLiteral)),
        (new(@"^(?<Value>-?0[bB][\d_]+)\s*"             , Options), typeof(NumericLiteral)),
        (new(@"^(?<Value>-?[\d_]+[.]?[\d_]*[fF])\s*"    , Options), typeof(NumericLiteral)),
        (new(@"^(?<Value>-?[\d_]+[.]?[\d_]*[dD]?)\s*"   , Options), typeof(NumericLiteral)),
        (new(@"^(?<Value>-?[\d_]+[.]?[\d_]*[mM])\s*"    , Options), typeof(NumericLiteral)),
        (new(@"^[+]=\s*"                                , Options), typeof(AddAssignOperator)),
        (new(@"^[+]\s*"                                 , Options), typeof(AddOperator)),
        (new(@"^=\s*"                                   , Options), typeof(AssignmentOperator)),
        (new(@"^&=\s*"                                  , Options), typeof(BitwiseAndAssignOperator)),
        (new(@"^&\s*"                                   , Options), typeof(BitwiseAndOperator)),
        (new(@"^~\s*"                                   , Options), typeof(BitwiseComplimentOperator)),
        (new(@"^\|=\s*"                                 , Options), typeof(BitwiseOrAssignOperator)),
        (new(@"^\|\s*"                                  , Options), typeof(BitwiseOrOperator)),
        (new(@"^--\s*"                                  , Options), typeof(DecrementOperator)),
        (new(@"^\\=\s*"                                 , Options), typeof(DivideAssignOperator)),
        (new(@"^\\\s*"                                  , Options), typeof(DivideOperator)),
        (new(@"^==\s*"                                  , Options), typeof(EqualsOperator)),
        (new(@"^\+\+\s*"                                , Options), typeof(IncrementOperator)),
        (new(@"^=>\s*"                                  , Options), typeof(LambdaOperator)),
        (new(@"^<<=\s*"                                 , Options), typeof(LeftShiftAssignOperator)),
        (new(@"^<<\s*"                                  , Options), typeof(LeftShiftOperator)),
        (new(@"^&&\s*"                                  , Options), typeof(LogicalAndOperator)),
        (new(@"^\|\|\s*"                                , Options), typeof(LogicalOrOperator)),
        (new(@"^%\s*"                                   , Options), typeof(ModOperator)),
        (new(@"^[*]=\s*"                                , Options), typeof(MultiplyAssignOperator)),
        (new(@"^[*]\s*"                                 , Options), typeof(MultiplyOperator)),
        (new(@"^!=\s*"                                  , Options), typeof(NotEqualOperator)),
        (new(@"^\?\?=\s*"                               , Options), typeof(NullCoalescingAssignmentOperator)),
        (new(@"^\?\?\s*"                                , Options), typeof(NullCoalescingOperator)),        
        (new(@"^>>=\s*"                                 , Options), typeof(RightShiftAssignOperator)),
        (new(@"^>>\s*"                                  , Options), typeof(RightShiftOperator)),
        (new(@"^-=\s*"                                  , Options), typeof(SubtractAssignOperator)),
        (new(@"^-\s*"                                   , Options), typeof(SubtractOperator)),
        (new(@"^\^=\s*"                                 , Options), typeof(XorAssignOperator)),
        (new(@"^\^\s*"                                  , Options), typeof(XorOperator)),
        (new(@"^>\s*"                                   , Options), typeof(CloseAngleBracketSymbol)),
        (new(@"^}\s*"                                   , Options), typeof(CloseBraceSymbol)),
        (new(@"^\)\s*"                                  , Options), typeof(CloseBracketSymbol)),
        (new(@"^]\s*"                                   , Options), typeof(CloseSquareBracketSymbol)),
        (new(@"^,\s*"                                   , Options), typeof(CommaSymbol)),
        (new(@"^[.]\s*"                                 , Options), typeof(DotSymbol)),
        (new(@"^<\s*"                                   , Options), typeof(OpenAngleBracketSymbol)),
        (new(@"^{\s*"                                   , Options), typeof(OpenBraceSymbol)),
        (new(@"^\(\s*"                                  , Options), typeof(OpenBracketSymbol)),
        (new(@"^\[\s*"                                  , Options), typeof(OpenSquareBracketSymbol)),
        (new(@"^;\s*"                                   , Options), typeof(TerminalSymbol)),
        (new(@"^include\s*"                             , Options), typeof(IncludeKeyword)),
        (new(@"^type\s*"                                , Options), typeof(TypeKeyword)),
        (new(@"^(?<Value>[A-Za-z][A-Za-z0-9_]*)\s*"     , Options), typeof(Name)),
    };
}
