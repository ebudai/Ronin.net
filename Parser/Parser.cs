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

        return ParseLiteral(Syntax.textliteral, Primitive.text)
            ?? ParseLiteral(Syntax.charliteral, Primitive.character)
            ?? ParseLiteral(Syntax.unicodeliteral, Primitive.character)
            ?? ParseHexLiteral()
            ?? ParseBinaryLiteral()
            ?? ParseLiteral(Syntax.halfliteral, Primitive.dec16)
            ?? ParseLiteral(Syntax.doubleliteral, Primitive.dec64)
            ?? ParseLiteral(Syntax.decimalliteral, Primitive.@decimal)
            ?? ParseLiteral(Syntax.moneyliteral, Primitive.money)
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

    private Literal ParseLiteral(Regex regex, string primitive)
    {
        var lexed = Lex(regex);
        return lexed is null ? null : new Literal { Value = lexed, Datatype = primitive };
    }

    private Literal ParseHexLiteral()
    {
        NumberStyles numberstyle = NumberStyles.AllowHexSpecifier | NumberStyles.AllowLeadingSign;

        var lexed = Lex(Syntax.hexliteral);
        if (lexed is null) return null;
        
        if (!BigInteger.TryParse(Clean(lexed), numberstyle, CultureInfo.CurrentCulture, out var value)) return null; // this should never happen

        cursor += lexed.Length;

        return new Literal
        {
            Value = lexed,
            Datatype = GetIntegerPrimitive(value)
        };

        static string Clean(string literal) 
        {
            int index = 0;
            if (literal.Length < Syntax.hexprefix.Length + 1) return literal;
            if (literal.StartsWith(Syntax.hexprefix)) index = Syntax.hexprefix.Length;
            else if (literal.StartsWith('-' + Syntax.hexprefix)) index = Syntax.hexprefix.Length + 1;
            return literal[index..].Replace("_", "");
        }
    }

    private Literal ParseIntegerLiteral()
    {
        NumberStyles numberstyle = NumberStyles.AllowLeadingSign; //TODO investigate using , for digit separator (in 3's) instead of _ whereever       NumberStyles.AllowThousands

        var lexed = Lex(Syntax.integerliteral)?.Replace("_", "");
        if (lexed is null) return null;

        if (!BigInteger.TryParse(lexed, numberstyle, CultureInfo.CurrentCulture, out var value)) throw new Exception($"{lexed} matched integer literal but BigInteger.TryParse() failed"); // this should never happen
        
        return new Literal 
        { 
            Value = lexed, 
            Datatype = GetIntegerPrimitive(value)
        };
    }

    private static string GetIntegerPrimitive(BigInteger value) =>
          value >= sbyte.MinValue && value <= sbyte.MaxValue ? Primitive.int8
        : value >= short.MinValue && value <= short.MaxValue ? Primitive.int16
        : value >= int.MinValue && value <= int.MaxValue ? Primitive.integer
        : value >= long.MinValue && value <= long.MaxValue ? Primitive.int64
        : Primitive.bigint;

    private Literal ParseBinaryLiteral()
    {
        var lexed = Lex(Syntax.binaryliteral);
        return lexed is null ? null : new Literal
        {
            Value = lexed,
            Datatype = (lexed.Length - Syntax.binaryprefix.Length) switch
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
        while (parameters.TryAdd(syntax, ref cursor)) syntax = ParseSyntax();
        
        if (syntax is Terminal)
        {
            cursor = originalcursor;
            return null;
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