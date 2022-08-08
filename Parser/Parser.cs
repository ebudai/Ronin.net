using Ronin.Parser.Grammar;
using Ronin.Parser.Grammar.Aggregates;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

using Object = Ronin.Parser.Grammar.Aggregates.Object;

namespace Ronin.Parser;

internal class Parser
{
    private readonly string sourcecode;
    private int line = 0;
    private int cursor = 0;

    internal Parser(FileInfo file) => sourcecode = Syntax.scopeopen + File.ReadAllText(file.FullName) + Syntax.scopeclose;

    internal Scope ParseScope()
    {
        var symbol = ParseSymbol();
        if (symbol is not OpeningBrace)
        {
            cursor -= symbol?.Value.Length ?? 0;
            return null;
        }

        Scope scope = new();

        while (scope.TryAdd(ParseExpression())) { }

        return scope;
    }

    private Expression ParseExpression()
    {
        Expression expression = new();

        while (expression.TryAdd(ParseSyntax(), ref cursor)) { }

        return expression.IsEmpty ? null : expression;
    }

    private Syntax ParseSyntax()
    {
        var match = Syntax.whitespace.Match(sourcecode, cursor);
        if (match.Success && match.Index == cursor)
        {
            cursor += match.Length;
            line += match.Value.Count(c => c == '\n');
        }

        if (cursor == sourcecode.Length) return null;

        return ParseTextLiteral(Syntax.textliteral, Primitive.text)
            ?? ParseTextLiteral(Syntax.charliteral, Primitive.character)
            ?? ParseTextLiteral(Syntax.unicodeliteral, Primitive.character)
            ?? ParseHexLiteral()
            ?? ParseBinaryLiteral()
            ?? ParseDecimalLiteral(Syntax.halfliteral, Primitive.dec16)
            ?? ParseDecimalLiteral(Syntax.doubleliteral, Primitive.dec64)
            ?? ParseDecimalLiteral(Syntax.decimalliteral, Primitive.@decimal)
            ?? ParseDecimalLiteral(Syntax.moneyliteral, Primitive.money)
            ?? ParseIntegerLiteral()
            ?? ParseParameters()
            ?? ParseAggregate<Object>(Syntax.groupingopen, Syntax.groupingclose)
            ?? ParseAggregate<List>(Syntax.listopen, Syntax.listclose)
            ?? ParseAggregate<Set>(Syntax.scopeopen, Syntax.scopeclose)
            ?? ParseScope()
            ?? ParseSymbol()
            ?? ParseKeyword()
            ?? ParseIdentifier() as Syntax
            ?? throw new Exception($"bad syntax at line {line}: {sourcecode[cursor..]}");
    }

    private Literal ParseTextLiteral(Regex regex, string primitive)
    {
        var lexed = Lex(regex);
        return lexed is null ? null : new Literal { Value = lexed, Datatype = primitive };
    }

    private Literal ParseDecimalLiteral(Regex regex, string primitive)
    {
        var lexed = Lex(regex)?.Replace("_", "");
        return lexed is null ? null : new Literal { Value = lexed, Datatype = primitive };
    }

    private Literal ParseHexLiteral()
    {
        var literal = Lex(Syntax.hexliteral)?.Replace("_", "")[Syntax.binaryprefix.Length..];
        if (literal is null) return null;

        var parsed = literal.Length is 1 ? '0' + literal : literal;

        if (!BigInteger.TryParse(parsed, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
        {
            throw new Exception($"{literal} matched hex literal but BigInteger.TryParse() failed"); // this should never happen
        }

        return new Literal
        {
            Value = literal,
            Datatype = value >= byte.MinValue && value <= byte.MaxValue ? Primitive.@byte
                : value >= ushort.MinValue && value <= ushort.MaxValue ? Primitive.bits16
                : value >= uint.MinValue && value <= uint.MaxValue ? Primitive.bits32
                : value >= ulong.MinValue && value <= ulong.MaxValue ? Primitive.bits64
                : Primitive.bitlist
        };
    }

    private Literal ParseIntegerLiteral()
    {
        NumberStyles numberstyle = NumberStyles.None; //TODO investigate using , for digit separator (in 3's) instead of _ whereever       NumberStyles.AllowThousands

        var literal = Lex(Syntax.integerliteral)?.Replace("_", "");
        if (literal is null) return null;

        var index = literal.IndexOf('i', StringComparison.OrdinalIgnoreCase);
        var parsed = index is -1 ? literal : literal[..index];
        if (!BigInteger.TryParse(parsed.TrimEnd(), numberstyle, CultureInfo.CurrentCulture, out var value))
        {
            throw new Exception($"{literal} matched integer literal but BigInteger.TryParse() failed"); // this should never happen
        }

        return new Literal
        {
            Value = literal,
            Datatype = literal.EndsWith("i8", StringComparison.OrdinalIgnoreCase) ? Primitive.int8
                : literal.EndsWith("i16", StringComparison.OrdinalIgnoreCase) ? Primitive.int16
                : literal.EndsWith("i64", StringComparison.OrdinalIgnoreCase) ? Primitive.int64
                : value <= long.MinValue || value >= long.MaxValue ? Primitive.bigint
                : value <= int.MinValue || value >= int.MaxValue ? Primitive.int64
                : Primitive.integer
        };
    }

    private Literal ParseBinaryLiteral()
    {
        var lexed = Lex(Syntax.binaryliteral)?.Replace("_", "")?[Syntax.binaryprefix.Length..];
        return lexed is null ? null : new Literal
        {
            Value = lexed,
            Datatype = lexed.Length switch
            {
                <= 8 => Primitive.@byte,
                <= 16 => Primitive.bits16,
                <= 32 => Primitive.bits32,
                <= 64 => Primitive.bits64,
                _ => Primitive.bitlist
            }
        };
    }

    private Symbol ParseSymbol()
    {
        var lexed = Lex(Syntax.symbol);
        return lexed is null ? null : Symbol.Get(lexed, this);
    }

    private Parameters ParseParameters()
    {
        var originalcursor = cursor;

        if (ParseSymbol() is not OpeningParenthesis)
        {
            cursor = originalcursor;
            return null;
        }
        
        Parameters parameters = new();
        var syntax = ParseSyntax();
        while (parameters.TryAdd(syntax, ref cursor))
        {
            if (syntax is Terminal or Literal)
            {
                cursor = originalcursor;
                return null;
            }
            syntax = ParseSyntax();            
        }
        
        return parameters;
    }

    private T ParseAggregate<T>(string open, string close) where T : Aggregate
    {
        var originalcursor = cursor;

        if (ParseSymbol()?.Value != open)
        {
            cursor = originalcursor;
            return null;
        }
        
        var aggregate = Activator.CreateInstance<T>();        
        Expression expression = new();
        var element = ParseSyntax();
        while (element is not Symbol symbol || symbol.Value != close)
        {
            if (element is Terminal)
            {
                cursor = originalcursor;
                return null;
            }
            
            if (element is Separator)
            {
                aggregate.Expressions.Add(expression);
                expression = new();
            }
            else if (!expression.TryAdd(element, ref cursor))
            {
               break;
            }

            element = ParseSyntax();
        }
        if (!expression.IsEmpty) aggregate.Expressions.Add(expression);
        return aggregate;
    }

    private Declaration ParseKeyword()
    {
        var lexed = Lex(Syntax.declaration);
        return lexed is null ? null : new(replacewhitespace.Replace(lexed, " "));
    }
    private static readonly Regex replacewhitespace = new(@"\s+", RegexOptions.Compiled);

    private Identifier ParseIdentifier()
    {
        var lexed = Lex(Syntax.identifier);
        return lexed is null ? null : new(lexed);
    }

    private string Lex(Regex regex)
    {
        var match = regex.Match(sourcecode, cursor);
        if (!match.Success || match.Index != cursor) return null;
        cursor += match.Length;
        line += match.Value.Count(c => c == '\n');
        return match.Value;
    }

    internal class ParseException : Exception
    {
        public ParseException(Parser parser, Exception inner = null)
                : base(CreateMessage(parser), inner) { }

        public ParseException(string message, Parser parser, Exception inner = null)
            : base(message + Environment.NewLine + CreateMessage(parser), inner) { }

        private static string CreateMessage(Parser parser, Exception inner = null)
        {
            var message = $"line {parser.line}: {parser.sourcecode[parser.cursor..(parser.cursor + 30)]}";
            if (inner is not null) message += $" caused by {inner.Message}";
            return message;
        }
    }
}