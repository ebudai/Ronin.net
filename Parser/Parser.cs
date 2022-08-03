using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Ronin.Parser;

internal static class Parser
{
    internal static Scope Parse(FileInfo file)
    {
        int line = 1;
        int cursor = 0;
        return ParseScope("{" + File.ReadAllText(file.FullName) + "}", ref cursor, ref line);
    }

    public class Exception : System.Exception
    {
        public Exception(string message) : base(message) { }
        public Exception(string message, System.Exception inner) : base(message, inner) { }
    }

    private static Scope ParseScope(string sourcecode, ref int cursor, ref int line)
    {
        if (!sourcecode.AsSpan(cursor).StartsWith("{")) return null;
        cursor += 1;
        Scope scope = new();

        var expression = ParseExpression(sourcecode, ref cursor, ref line);
        while (expression.Syntax.Count > 0)
        {
            scope.Expressions.Add(expression);
            expression = ParseExpression(sourcecode, ref cursor, ref line);
            if (sourcecode.AsSpan(cursor).StartsWith("}"))
            {
                if (expression.Syntax.Count > 0) scope.Expressions.Add(expression);
                cursor += 1;
                return scope;
            }
        }

        throw new Exception("expected }");
    }

    private static Expression ParseExpression(string sourcecode, ref int cursor, ref int line)
    {
        Expression expression = new();

        var syntax = ParseSyntax(sourcecode, ref cursor, ref line);
        while (syntax is not Symbol)
        {
            if (expression.Syntax.LastOrDefault() is Identifier identifier)
            {
                if (syntax is Literal literal) identifier.Add(literal);
                else if (syntax is Identifier moreidentifier) identifier.Add(moreidentifier);
                else expression.Syntax.Add(syntax);
            }
            else if (syntax is Keyword keyword && expression.Syntax.LastOrDefault() is Scope)
            {
                cursor -= keyword.ToString().Length;
                return expression;
            }
            else
            {
                expression.Syntax.Add(syntax);
            }
            syntax = ParseSyntax(sourcecode, ref cursor, ref line);
        }

        if (syntax is ClosingBrace brace) cursor -= brace.Value.Length;

        return expression;
    }

    private static Syntax ParseSyntax(string sourcecode, ref int cursor, ref int line)
    {
        var match = Syntax.whitespace.Match(sourcecode, cursor);
        if (match.Success && match.Index == cursor)
        {
            cursor += match.Length;
            line += match.Value.Count(c => c == '\n');
        }

        if (cursor == sourcecode.Length) return null;

        return ParseLiteral(sourcecode, ref cursor, ref line, Syntax.textliteral, Primitive.text)
            ?? ParseLiteral(sourcecode, ref cursor, Syntax.charliteral, Primitive.character)
            ?? ParseLiteral(sourcecode, ref cursor, Syntax.unicodeliteral, Primitive.character)
            ?? ParseIntegerLiteral(sourcecode, ref cursor, Syntax.hexliteral, NumberStyles.AllowHexSpecifier, "0x")
            ?? ParseBinaryLiteral(sourcecode, ref cursor)
            ?? ParseLiteral(sourcecode, ref cursor, Syntax.halfliteral, Primitive.dec16)
            ?? ParseLiteral(sourcecode, ref cursor, Syntax.doubleliteral, Primitive.dec64)
            ?? ParseLiteral(sourcecode, ref cursor, Syntax.decimalliteral, Primitive.@decimal)
            ?? ParseLiteral(sourcecode, ref cursor, Syntax.moneyliteral, Primitive.money)
            ?? ParseIntegerLiteral(sourcecode, ref cursor, Syntax.integerliteral, NumberStyles.None)
            ?? ParseSymbol(sourcecode, ref cursor)
            ?? ParseParameters(sourcecode, ref cursor, ref line)
            ?? ParseAggregate<ObjectLiteral>(sourcecode, ref cursor, ref line, "(", ")")
            ?? ParseAggregate<ListLiteral>(sourcecode, ref cursor, ref line, "[", "]")
            ?? ParseAggregate<SetLiteral>(sourcecode, ref cursor, ref line, "{", "}")
            ?? ParseScope(sourcecode, ref cursor, ref line)
            ?? ParseKeyword(sourcecode, ref cursor, ref line)
            ?? ParseIdentifier(sourcecode, ref cursor) as Syntax
            ?? throw new Exception($"bad syntax at line {line}: {sourcecode[cursor..]}");
    }

    private static Literal ParseLiteral(string sourcecode, ref int cursor, Regex regex, string primitive)
    {
        var match = regex.Match(sourcecode, cursor);
        if (!match.Success || match.Index != cursor) return null;
        cursor += match.Length;
        return new Literal { Value = match.Value, Datatype = primitive };
    }

    private static Literal ParseLiteral(string sourcecode, ref int cursor, ref int line, Regex regex, string primitive)
    {
        var match = regex.Match(sourcecode, cursor);
        if (!match.Success || match.Index != cursor) return null;
        cursor += match.Length;
        line += match.Value.Count(c => c == '\n');
        return new Literal { Value = match.Value, Datatype = primitive };
    }

    private static Literal ParseIntegerLiteral(string sourcecode, ref int cursor, Regex regex, NumberStyles numberstyle, string prefix = "")
    {
        var match = regex.Match(sourcecode, cursor);
        if (!match.Success || match.Index != cursor) return null;
        numberstyle |= NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign;
        string text = match.Value[prefix.Length..];
        if (match.Value[0] == '-') text = '-' + text[1..];
        if (!BigInteger.TryParse(text.Replace("_", ""), numberstyle, CultureInfo.CurrentCulture, out var value)) return null; // this should never happen
        cursor += match.Length;
        return new Literal 
        { 
            Value = match.Value, 
            Datatype = value >= sbyte.MinValue && value <= sbyte.MaxValue ? Primitive.int8
                : value >= short.MinValue && value <= short.MaxValue ? Primitive.int16
                : value >= int.MinValue && value <= int.MaxValue ? Primitive.integer
                : value >= long.MinValue && value <= long.MaxValue ? Primitive.int64
                : Primitive.bigint
        };
    }

    private static Literal ParseBinaryLiteral(string sourcecode, ref int cursor)
    {
        var match = Syntax.binaryliteral.Match(sourcecode, cursor);
        if (!match.Success || match.Index != cursor) return null;
        cursor += match.Length;
        return new Literal
        {
            Value = match.Value,
            Datatype = (match.Length - 2) switch
            {
                <= 8 => Primitive.@byte,
                <= 16 => Primitive.bits16,
                <= 32 => Primitive.bits32,
                <= 64 => Primitive.bits64,
                _ => Primitive.bitlist
            }
        };
    }

    private static Symbol ParseSymbol(string sourcecode, ref int cursor)
    {
        var match = Syntax.symbol.Match(sourcecode, cursor);
        if (!match.Success || match.Index != cursor) return null;
        cursor += match.Length;
        return Symbol.Get(match.Value);
    }

    private static Parameters ParseParameters(string sourcecode, ref int cursor, ref int line)
    {
        if (!sourcecode.AsSpan(cursor).StartsWith("(")) return null;
        var originalcursor = cursor;
        cursor += 1;
        Parameters parameters = new();
        Identifier variable = null;        
        while (cursor < sourcecode.Length)
        {
            var syntax = ParseSyntax(sourcecode, ref cursor, ref line);
            if (syntax is null || syntax is Terminal)
            {
                cursor = originalcursor;
                return null;
            }

            if (syntax is Identifier name)
            {
                variable?.Add(name);
                variable ??= name;
            }
            else if (syntax is Separator)
            {
                parameters.Variables.Add(variable);
                variable = null;
            }
            else if (syntax is ClosingParenthesis)
            {
                parameters.Variables.Add(variable);
                break;
            }
            else if (syntax is ListLiteral listliteral)
            {
                // if the form is [datatype name], tnen we have a map literal instead
                if (listliteral.Expressions.FirstOrDefault()?.Syntax.FirstOrDefault() is Identifier identifier)
                {
                    var datatypename = identifier.ToString();
                    if (datatypename is not "") syntax = new MapLiteral { KeyDatatype = datatypename };
                }
                variable.Add(syntax);
            }
            else
            {
                parameters.Variables.Add(variable);
            }
        }
        return parameters;
    }

    private static T ParseAggregate<T>(string sourcecode, ref int cursor, ref int line, string open, string close) where T : Aggregate
    {
        if (!sourcecode.AsSpan(cursor).StartsWith(open)) return null;
        var originalcursor = cursor;
        cursor += open.Length;
        var aggregate = Activator.CreateInstance<T>();
        
        Expression expression = new();
        while (cursor < sourcecode.Length)
        {
            var element = ParseSyntax(sourcecode, ref cursor, ref line);
            if (element is Terminal)
            {
                cursor = originalcursor;
                return null;
            }
            else if (element is Separator)
            {
                aggregate.Expressions.Add(expression);
                expression = new();
            }
            else if (element is Symbol symbol && symbol.Value == close)
            {
                aggregate.Expressions.Add(expression);
                break;
            }
            else
            {
                expression.Syntax.Add(element);
            }
        }
        return aggregate;
    }

    private static Keyword ParseKeyword(string sourcecode, ref int cursor, ref int line)
    {
        var match = Syntax.keyword.Match(sourcecode, cursor);
        if (!match.Success || match.Index != cursor) return null;
        cursor += match.Length;
        line += match.Value.Count(c => c == '\n');
        return new(replacespaces.Replace(match.Value, " "));
    }
    private static readonly Regex replacespaces = new(@"\s+", RegexOptions.Compiled);

    private static Identifier ParseIdentifier(string sourcecode, ref int cursor)
    {
        var match = Syntax.identifier.Match(sourcecode, cursor);
        if (!match.Success || match.Index != cursor) return null;
        cursor += match.Length;
        return new(match.Value);
    }
}